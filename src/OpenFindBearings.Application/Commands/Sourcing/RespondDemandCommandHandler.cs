using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Sourcing
{
    /// <summary>
    /// 应答型号行输入（v1.5.0 多行标书）：一条应答含多行型号，每行可引用在售商品或自由手填。
    /// 发布人需求明确时商家填一行（通常引用在售）；需求模糊时商家把自己认为相关的型号挨个列多行
    /// </summary>
    /// <param name="PartNumber">型号（必填，平台库外的自由文本也可）</param>
    /// <param name="BearingId">引用的在售商品 ID（可空=自由文本型号行）</param>
    /// <param name="Price">该行报价（可选）</param>
    /// <param name="Stock">该行库存描述（可选）</param>
    /// <param name="LeadTime">该行交期描述（可选）</param>
    public record SourcingResponseItemInput(
        string PartNumber, Guid? BearingId, decimal? Price, string? Stock, string? LeadTime);

    /// <summary>
    /// 应答寻货需求命令（v1.35.0）：商户对进行中需求提交报价应答。
    /// 归属判定是商户（应答=商家行为），操作人 userId 仅审计；一商户一需求只一条应答，
    /// 重复提交=更新既有应答（不占额度不重复通知）；额度模型与发布同构（免费 20/天→积分加量→硬上限）。
    /// v1.5.0 多行标书：报价/库存/交期按行携带（Items），说明 Remark 整份必填
    /// </summary>
    /// <param name="UserId">操作人（商户成员）</param>
    /// <param name="MerchantId">应答商户</param>
    /// <param name="DemandId">寻货需求</param>
    /// <param name="Items">应答型号行（至少一行，每行型号必填）</param>
    /// <param name="Remark">应答说明（必填一句话）</param>
    /// <param name="UsePoints">免费额度用完后确认花积分</param>
    public record RespondDemandCommand(
        Guid UserId, Guid MerchantId, Guid DemandId, List<SourcingResponseItemInput> Items,
        string Remark, bool UsePoints) : IRequest;

    /// <summary>
    /// 应答寻货处理器
    /// </summary>
    public class RespondDemandCommandHandler : IRequestHandler<RespondDemandCommand>
    {
        private readonly ISourcingDemandRepository _demandRepository;
        private readonly ISourcingResponseRepository _responseRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantMemberRepository _memberRepository;
        private readonly ISystemConfigRepository _configRepository;
        private readonly IPointGrantRuleRepository _ruleRepository;
        private readonly IPointsService _pointsService;
        private readonly IMediator _mediator;
        // 改动说明（v2.21.0 型号线索）：应答型号行未收录时累积线索，供管理员判断是否建档
        private readonly IBearingLeadRepository _bearingLeadRepository;
        private readonly ILogger<RespondDemandCommandHandler> _logger;

        /// <summary>
        /// 构造：寻货/商户/配置/积分仓储服务 + 事件派发
        /// </summary>
        public RespondDemandCommandHandler(
            ISourcingDemandRepository demandRepository,
            ISourcingResponseRepository responseRepository,
            IMerchantRepository merchantRepository,
            IMerchantMemberRepository memberRepository,
            ISystemConfigRepository configRepository,
            IPointGrantRuleRepository ruleRepository,
            IPointsService pointsService,
            IMediator mediator,
            IBearingLeadRepository bearingLeadRepository,
            ILogger<RespondDemandCommandHandler> logger)
        {
            _demandRepository = demandRepository;
            _responseRepository = responseRepository;
            _merchantRepository = merchantRepository;
            _memberRepository = memberRepository;
            _configRepository = configRepository;
            _ruleRepository = ruleRepository;
            _pointsService = pointsService;
            _mediator = mediator;
            _bearingLeadRepository = bearingLeadRepository;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task Handle(RespondDemandCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Remark))
                throw new InvalidOperationException("请填写应答说明");
            // 改动说明（v1.5.0 多行标书）：应答至少一行型号，每行型号必填——空标书禁止提交
            if (request.Items == null || request.Items.Count == 0)
                throw new InvalidOperationException("请至少填写一个型号");
            if (request.Items.Any(i => string.IsNullOrWhiteSpace(i.PartNumber)))
                throw new InvalidOperationException("每个型号行都必须填写型号");

            var demand = await _demandRepository.GetByIdAsync(request.DemandId, cancellationToken)
                ?? throw new InvalidOperationException("寻货需求不存在");
            // 读时惰性过期兜底：过期单不接受应答
            if (demand.TryExpire())
                await _demandRepository.UpdateAsync(demand, cancellationToken);
            if (!demand.IsOpen)
                throw new InvalidOperationException("该寻货已结束，无法应答");

            // 改动说明（v1.57.3 自交易封堵）：自己不能应答自己发布的寻货——个人名义发布、
            //   商户名义发布与公开大厅应答三条路径统一在此拦截（邀请名单已在发布侧排除自家店铺）
            if (demand.PublisherUserId == request.UserId)
                throw new InvalidOperationException("不能应答自己发布的寻货");
            if (demand.PublisherMerchantId == request.MerchantId)
                throw new InvalidOperationException("不能应答自己商户发布的寻货");

            // 改动说明（v1.8.1 邀请边界）：邀请单只允许收件商家应答——详情端点已对非收件商户 404，
            //   应答链路同样需要守卫，否则知道 id 的第三方可插队应答，邀请边界（只邀这几家）被击穿；
            //   判定基于发布时点快照而非"当前是否仍有货"，避免受邀商家事后下架就被拒
            if (demand.IsPrivate && !demand.IsInvitedTo(request.MerchantId))
                throw new InvalidOperationException("该寻货为邀请单，仅受邀商家可应答");

            // 应答资格：操作人须为该商户在职成员（员工可代商户应答），商户须生效
            var member = await _memberRepository.GetActiveByUserAndMerchantAsync(
                request.UserId, request.MerchantId, cancellationToken)
                ?? throw new InvalidOperationException("您不是该商户的在职成员");
            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken)
                ?? throw new InvalidOperationException("商户不存在");
            if (merchant.Status != Domain.Enums.MerchantStatus.Active)
                throw new InvalidOperationException("商户当前状态不允许应答");

            // 改动说明（v1.57.3 自交易封堵）：发布人本人是应答商户在职成员时同样禁止——
            //   堵住"同事用个人名义发布、本店接单"的绕行路径
            if (await _memberRepository.GetActiveByUserAndMerchantAsync(
                demand.PublisherUserId, request.MerchantId, cancellationToken) != null)
                throw new InvalidOperationException("不能应答自己所在的商户");

            // 重复应答=更新既有（不占额度、不再发事件）
            // 改动说明（v1.8.2 撤销应答软取消）：已撤销（Cancelled）的既有行走"复活"而非新插一行——
            //   撤销后行仍在（兑现"额度不回退"），故必须复用同一行，否则撞 (DemandId,MerchantId) 唯一约束
            var existing = await _responseRepository.GetByDemandAndMerchantAsync(
                request.DemandId, request.MerchantId, cancellationToken);
            if (existing != null)
            {
                if (existing.Status == SourcingResponseStatus.Cancelled)
                {
                    // 复活：状态回待处理 + 覆盖说明，型号行整体替换；需求应答冗余列同步 +1（撤销时已 -1）
                    existing.Revive(request.Remark);
                    await _responseRepository.UpdateAsync(existing, cancellationToken);
                    await _responseRepository.DeleteItemsAsync(existing.Id, cancellationToken);
                    await _responseRepository.AddItemsAsync(
                        BuildItems(existing.Id, request.Items), cancellationToken);
                    demand.IncrementResponseCount();
                    await _demandRepository.UpdateAsync(demand, cancellationToken);
                    // 改动说明（v2.21.0 型号线索）：复活应答同样记录未收录型号
                    await RecordItemLeadsAsync(request.Items, cancellationToken);
                    return;
                }
                existing.UpdateContent(request.Remark);
                await _responseRepository.UpdateAsync(existing, cancellationToken);
                // 型号行整体替换：先清旧行再写新行（显式 Add，规避 EF 导航陷阱）
                await _responseRepository.DeleteItemsAsync(existing.Id, cancellationToken);
                await _responseRepository.AddItemsAsync(
                    BuildItems(existing.Id, request.Items), cancellationToken);
                // 改动说明（v2.21.0 型号线索）：更新应答同样记录未收录型号
                await RecordItemLeadsAsync(request.Items, cancellationToken);
                return;
            }

            // 额度评估（商户维度）
            var today = await _responseRepository.CountRespondedTodayAsync(request.MerchantId, cancellationToken);
            var freeLimit = await SourcingConfigReader.GetIntAsync(_configRepository, "Sourcing.FreeRespondPerDay", 20);
            // 改动说明（v2.19.0 审计修复）：应答免费额度按商户牌级加成，仅铜牌及以上生效
            // （额度按商户维度计数，buff 口径与之对齐——铜牌 +1 / 银牌 +3 / 金牌 +5；普通档无加成）
            freeLimit += MerchantBuffs.RespondQuotaBonus((int)merchant.Grade);
            var rule = await _ruleRepository.GetEnabledByTypeAsync("sourcing_respond_bonus", cancellationToken);
            var hardLimit = rule?.DailyLimit ?? 50;
            var price = rule?.Amount ?? 20;
            var (decision, cost) = SourcingQuotaHelper.Evaluate(today, freeLimit, hardLimit, request.UsePoints, price);

            switch (decision)
            {
                case QuotaDecision.Rejected:
                    throw new InvalidOperationException($"今日应答已达上限（{hardLimit} 条），明天请早");
                case QuotaDecision.NeedPoints:
                    throw new InvalidOperationException($"NEED_POINTS:{cost}");
            }

            if (cost > 0)
            {
                // 改动说明（v1.8.2 彻底幂等）：扣分幂等键取"需求+商户"确定性键——一商户一需求终身只可能
                //   在首次创建时扣一次（重复应答走更新分支不扣），并发的重复提交亦被该键去重，不会重复扣分
                await _pointsService.DeductAsync(request.UserId, "sourcing_respond_bonus", cost,
                    $"sourcing_respond:{request.DemandId}:{request.MerchantId}", "寻货应答积分加量", cancellationToken);
            }

            var response = SourcingResponse.Create(request.DemandId, request.MerchantId, request.UserId,
                request.Remark);
            await _responseRepository.AddAsync(response, cancellationToken);
            await _responseRepository.AddItemsAsync(
                BuildItems(response.Id, request.Items), cancellationToken);
            demand.IncrementResponseCount();
            await _demandRepository.UpdateAsync(demand, cancellationToken);

            // 通知发布人（事件在 UnitOfWork 提交成功后由本 handler 手动派发——
            //   与 NominationAcceptedEvent 同款模式；此处 Publish 在 SaveChanges 前入队，
            //   由管道统一提交后派发不适用，故走 mediator 直发，订阅者读库时行已可见？）
            // 改动说明：直发时序问题——PointsService 独立提交先落，本命令的应答行要等管道 SaveChanges；
            //   订阅者只发站内信（不读应答表），无时序依赖，安全
            await _mediator.Publish(new SourcingRespondedEvent(
                demand.Id, demand.PublisherUserId, merchant.Id, merchant.Name, demand.PartNumber,
                // 改动说明（v1.6.0）：商户名义发布时透出发布商户，订阅者据此通知该商户全部在职成员
                demand.PublisherMerchantId), cancellationToken);

            // 改动说明（v2.7.0 G2 三件套）：应答成功（新增，非更新）是三项之一，
            // 触发补发判定——操作人（商户成员）当日已签到+纠错采纳时补齐 combo
            await _pointsService.TryGrantDailyComboAsync(request.UserId, cancellationToken);

            // 改动说明（v2.21.0 型号线索）：新建应答记录未收录型号（自由文本行且型号库无此型号）
            await RecordItemLeadsAsync(request.Items, cancellationToken);
        }

        /// <summary>
        /// 记录应答型号行中的未收录型号线索
        /// 改动说明（v2.21.0 型号线索）：只记录"平台型号库根本没有"的型号，型号存在但无人公域在售不算线索；
        /// 线索写入失败只记日志，不影响应答主流程
        /// </summary>
        private async Task RecordItemLeadsAsync(IEnumerable<SourcingResponseItemInput> items, CancellationToken cancellationToken)
        {
            foreach (var item in items)
            {
                if (item.BearingId.HasValue || string.IsNullOrWhiteSpace(item.PartNumber))
                    continue;

                try
                {
                    var partNumberKey = BearingLead.NormalizeKey(item.PartNumber);
                    if (!await _bearingLeadRepository.ExistsInLibraryAsync(partNumberKey, cancellationToken))
                    {
                        await _bearingLeadRepository.RecordAsync(
                            item.PartNumber, null, BearingLeadSource.Response, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "记录应答型号线索失败: {PartNumber}", item.PartNumber);
                }
            }
        }

        /// <summary>
        /// 构建应答型号行列表（v1.5.0 多行标书）：型号去空白，引用的在售商品 ID 透传
        /// </summary>
        private static List<SourcingResponseItem> BuildItems(Guid responseId, IEnumerable<SourcingResponseItemInput> inputs)
        {
            return inputs.Select(i => SourcingResponseItem.Create(
                responseId, i.PartNumber, i.BearingId, i.Price, i.Stock, i.LeadTime)).ToList();
        }
    }
}
