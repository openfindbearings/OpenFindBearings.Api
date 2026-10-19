using MediatR;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Application.Shared.Interfaces;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Sourcing
{
    /// <summary>
    /// 提交/更新成交回执命令（v2.18.0 撮合成交链）：需求方对被选定商户一键回填洽谈结果。
    /// 只有"谈成"且通过反刷校验（自关联/配对去重/资格门槛/异常检测）才计分，
    /// 计分成交是商家卡级"成功撮合"的唯一来源；重复提交=更新（幂等，覆盖旧回执）。
    /// </summary>
    /// <param name="UserId">当前操作用户（须为需求方本人或其发布商户成员）</param>
    /// <param name="ResponseId">被选定的应答标识</param>
    /// <param name="Outcome">洽谈结果枚举值（0=谈成，1-4=失败原因）</param>
    /// <param name="Reason">补充说明（可选，如"其他"的文字描述）</param>
    /// <param name="Ip">提交来源 IP（异常检测留痕，可为空）</param>
    public record SubmitDealFeedbackCommand(Guid UserId, Guid ResponseId, int Outcome, string? Reason, string? Ip)
        : IRequest<DealFeedbackResult>;

    /// <summary>
    /// 成交回执提交结果（是否计入卡级 + 未计入原因，供前端提示与后台留痕）
    /// </summary>
    /// <param name="Counted">是否计入商家卡级</param>
    /// <param name="Note">未计入原因（可空；仅在未计入时有值）</param>
    public record DealFeedbackResult(bool Counted, string? Note);

    /// <summary>
    /// 成交回执处理器：校验身份与选定关系 → 反刷四重校验 → 落库回执 → 计分则触发商家卡级重算
    /// </summary>
    public class SubmitDealFeedbackCommandHandler : IRequestHandler<SubmitDealFeedbackCommand, DealFeedbackResult>
    {
        private readonly ISourcingResponseRepository _responses;
        private readonly ISourcingDemandRepository _demands;
        private readonly IMerchantMemberRepository _members;
        private readonly IMerchantRepository _merchants;
        private readonly IUserRepository _users;
        private readonly ISystemConfigRepository _configs;
        private readonly IMerchantGradeService _grades;
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// 构造：寻货/商户/成员/用户仓储 + 配置 + 卡级服务 + 工作单元
        /// </summary>
        public SubmitDealFeedbackCommandHandler(
            ISourcingResponseRepository responses,
            ISourcingDemandRepository demands,
            IMerchantMemberRepository members,
            IMerchantRepository merchants,
            IUserRepository users,
            ISystemConfigRepository configs,
            IMerchantGradeService grades,
            IUnitOfWork unitOfWork)
        {
            _responses = responses;
            _demands = demands;
            _members = members;
            _merchants = merchants;
            _users = users;
            _configs = configs;
            _grades = grades;
            _unitOfWork = unitOfWork;
        }

        /// <inheritdoc/>
        public async Task<DealFeedbackResult> Handle(SubmitDealFeedbackCommand request, CancellationToken cancellationToken)
        {
            if (!Enum.IsDefined(typeof(SourcingDealOutcome), request.Outcome))
                throw new InvalidOperationException("回执结果不合法");
            var outcome = (SourcingDealOutcome)request.Outcome;

            var response = await _responses.GetByIdAsync(request.ResponseId, cancellationToken)
                ?? throw new InvalidOperationException("应答不存在");
            var demand = await _demands.GetByIdAsync(response.DemandId, cancellationToken)
                ?? throw new InvalidOperationException("寻货需求不存在");

            // 身份校验：需求方本人，或商户名义发布时的发布商户在职成员
            var isPublisher = demand.PublisherUserId == request.UserId;
            if (!isPublisher && demand.PublisherMerchantId.HasValue)
            {
                var pubMembership = await _members.GetActiveByUserAndMerchantAsync(
                    request.UserId, demand.PublisherMerchantId.Value, cancellationToken);
                isPublisher = pubMembership != null;
            }
            if (!isPublisher)
                throw new UnauthorizedAccessException("只有需求方可以提交成交回执");

            // 仅可对被选定（Adopted）的应答回执——回执锚定在"合作达成"之上
            if (demand.SelectedResponseId != response.Id || response.Status != SourcingResponseStatus.Adopted)
                throw new InvalidOperationException("仅可对被选定的合作提交回执");

            // 只有"谈成"参与计分；失败原因作为负向信号留痕供管理侧复核，不计分（降级由滚动窗口自然衰减承担）
            var counted = false;
            string? note = null;
            if (outcome == SourcingDealOutcome.Confirmed)
            {
                var windowDays = await _configs.GetValueAsync<int?>(MerchantGradeSettings.WindowDaysKey, null) ?? MerchantGradeSettings.WindowDaysDefault;
                (counted, note) = await EvaluateAntiFarmAsync(demand, response, request, windowDays, cancellationToken);
            }

            response.SubmitFeedback(outcome, request.Reason, counted, note, request.Ip);
            await _responses.UpdateAsync(response, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 计分变动可能改变成功撮合次数 → 触发商家卡级重算（服务内部吞失败，不影响回执落库）
            if (counted)
                await _grades.RecomputeAsync(response.MerchantId, cancellationToken);

            return new DealFeedbackResult(counted, note);
        }

        /// <summary>
        /// 反刷四重校验（v2.18.0）：返回（是否计分, 未计分原因）。
        /// 任一命中即不计分并写留痕，保证卡级"成功撮合"来自真实的、非关联的、合格的撮合。
        /// </summary>
        private async Task<(bool Counted, string? Note)> EvaluateAntiFarmAsync(
            Domain.Aggregates.SourcingDemand demand, SourcingResponse response,
            SubmitDealFeedbackCommand request, int windowDays, CancellationToken cancellationToken)
        {
            // 1 自关联排除：发布人就是被选商户的在职成员（自己给自己回执）
            var selfMembership = await _members.GetActiveByUserAndMerchantAsync(
                request.UserId, response.MerchantId, cancellationToken);
            if (selfMembership != null)
                return (false, "发布方与被选商户存在成员关联（疑似自成交）");

            // 2 联系方式重合排除：需求方与商户登记电话相同（疑似同一主体多马甲）
            var target = await _merchants.GetByIdAsync(response.MerchantId, cancellationToken);
            var targetPhone = target?.Contact?.Mobile ?? target?.Contact?.Phone;
            string? publisherPhone;
            if (demand.PublisherMerchantId.HasValue)
            {
                var pubMerchant = await _merchants.GetByIdAsync(demand.PublisherMerchantId.Value, cancellationToken);
                publisherPhone = pubMerchant?.Contact?.Mobile ?? pubMerchant?.Contact?.Phone;
            }
            else
            {
                var publisher = await _users.GetByIdAsync(demand.PublisherUserId, cancellationToken);
                publisherPhone = publisher?.Mobile;
            }
            if (!string.IsNullOrWhiteSpace(targetPhone) && targetPhone == publisherPhone)
                return (false, "需求方与商户登记电话相同（疑似关联方）");

            // 3 资格门槛：账号绑定手机（近实名代理）且注册满 N 天；真实实名在认证中心，API 侧以手机绑定为代理
            var minAgeDays = await _configs.GetValueAsync<int?>(SourcingFeedbackSettings.MinAccountAgeDaysKey, null)
                ?? SourcingFeedbackSettings.MinAccountAgeDaysDefault;
            var publisherUser = await _users.GetByIdAsync(demand.PublisherUserId, cancellationToken);
            if (publisherUser != null)
            {
                if (string.IsNullOrWhiteSpace(publisherUser.Mobile))
                    return (false, "需求方账号未绑定手机号");
                if (minAgeDays > 0 && publisherUser.CreatedAt > DateTime.UtcNow.AddDays(-minAgeDays))
                    return (false, $"需求方账号注册未满 {minAgeDays} 天");
            }

            // 4 异常检测：同 IP 时间窗内计分回执过于集中（疑似脚本批量刷分）
            if (!string.IsNullOrWhiteSpace(request.Ip))
            {
                var ipWindowHours = await _configs.GetValueAsync<int?>(SourcingFeedbackSettings.IpWindowHoursKey, null)
                    ?? SourcingFeedbackSettings.IpWindowHoursDefault;
                var ipMax = await _configs.GetValueAsync<int?>(SourcingFeedbackSettings.IpMaxCountKey, null)
                    ?? SourcingFeedbackSettings.IpMaxCountDefault;
                var recentFromIp = await _responses.CountFeedbackByIpSinceAsync(
                    request.Ip, DateTime.UtcNow.AddHours(-ipWindowHours), cancellationToken);
                if (recentFromIp >= ipMax)
                    return (false, $"同 IP 在 {ipWindowHours} 小时内计分回执已达 {ipMax} 条（疑似刷分）");
            }

            // 5 配对去重：同一（发布人, 商户）关系在窗口内只计一次，防反复刷
            var duplicated = await _responses.HasCountedDealAsync(
                response.MerchantId, demand.PublisherUserId, DateTime.UtcNow.AddDays(-windowDays), cancellationToken);
            if (duplicated)
                return (false, "该需求方与本商户在统计窗口内已有计分成交（去重）");

            return (true, null);
        }
    }
}
