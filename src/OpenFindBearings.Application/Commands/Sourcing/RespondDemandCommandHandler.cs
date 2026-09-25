using MediatR;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Sourcing
{
    /// <summary>
    /// 应答寻货需求命令（v1.35.0）：商户对进行中需求提交报价应答。
    /// 归属判定是商户（应答=商家行为），操作人 userId 仅审计；一商户一需求只一条应答，
    /// 重复提交=更新既有应答（不占额度不重复通知）；额度模型与发布同构（免费 20/天→积分加量→硬上限）
    /// </summary>
    /// <param name="UserId">操作人（商户成员）</param>
    /// <param name="MerchantId">应答商户</param>
    /// <param name="DemandId">寻货需求</param>
    /// <param name="Price">报价单价（可选）</param>
    /// <param name="Stock">库存描述（可选）</param>
    /// <param name="LeadTime">交期描述（可选）</param>
    /// <param name="Remark">应答说明（必填一句话）</param>
    /// <param name="UsePoints">免费额度用完后确认花积分</param>
    public record RespondDemandCommand(
        Guid UserId, Guid MerchantId, Guid DemandId, decimal? Price, string? Stock,
        string? LeadTime, string Remark, bool UsePoints) : IRequest;

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
            IMediator mediator)
        {
            _demandRepository = demandRepository;
            _responseRepository = responseRepository;
            _merchantRepository = merchantRepository;
            _memberRepository = memberRepository;
            _configRepository = configRepository;
            _ruleRepository = ruleRepository;
            _pointsService = pointsService;
            _mediator = mediator;
        }

        /// <inheritdoc/>
        public async Task Handle(RespondDemandCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Remark))
                throw new InvalidOperationException("请填写应答说明");

            var demand = await _demandRepository.GetByIdAsync(request.DemandId, cancellationToken)
                ?? throw new InvalidOperationException("寻货需求不存在");
            // 读时惰性过期兜底：过期单不接受应答
            if (demand.TryExpire())
                await _demandRepository.UpdateAsync(demand, cancellationToken);
            if (!demand.IsOpen)
                throw new InvalidOperationException("该寻货已结束，无法应答");

            // 应答资格：操作人须为该商户在职成员（员工可代商户应答），商户须生效
            var member = await _memberRepository.GetActiveByUserAndMerchantAsync(
                request.UserId, request.MerchantId, cancellationToken)
                ?? throw new InvalidOperationException("您不是该商户的在职成员");
            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken)
                ?? throw new InvalidOperationException("商户不存在");
            if (merchant.Status != Domain.Enums.MerchantStatus.Active)
                throw new InvalidOperationException("商户当前状态不允许应答");

            // 重复应答=更新既有（不占额度、不再发事件）
            var existing = await _responseRepository.GetByDemandAndMerchantAsync(
                request.DemandId, request.MerchantId, cancellationToken);
            if (existing != null)
            {
                existing.UpdateContent(request.Price, request.Stock, request.LeadTime, request.Remark);
                await _responseRepository.UpdateAsync(existing, cancellationToken);
                return;
            }

            // 额度评估（商户维度）
            var today = await _responseRepository.CountRespondedTodayAsync(request.MerchantId, cancellationToken);
            var freeLimit = await SourcingConfigReader.GetIntAsync(_configRepository, "Sourcing.FreeRespondPerDay", 20);
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
                await _pointsService.DeductAsync(request.UserId, "sourcing_respond_bonus", cost,
                    null, "寻货应答积分加量", cancellationToken);
            }

            var response = SourcingResponse.Create(request.DemandId, request.MerchantId, request.UserId,
                request.Price, request.Stock, request.LeadTime, request.Remark);
            await _responseRepository.AddAsync(response, cancellationToken);
            demand.IncrementResponseCount();
            await _demandRepository.UpdateAsync(demand, cancellationToken);

            // 通知发布人（事件在 UnitOfWork 提交成功后由本 handler 手动派发——
            //   与 NominationAcceptedEvent 同款模式；此处 Publish 在 SaveChanges 前入队，
            //   由管道统一提交后派发不适用，故走 mediator 直发，订阅者读库时行已可见？）
            // 改动说明：直发时序问题——PointsService 独立提交先落，本命令的应答行要等管道 SaveChanges；
            //   订阅者只发站内信（不读应答表），无时序依赖，安全
            await _mediator.Publish(new SourcingRespondedEvent(
                demand.Id, demand.PublisherUserId, merchant.Id, merchant.Name, demand.PartNumber), cancellationToken);
        }
    }
}
