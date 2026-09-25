using MediatR;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Sourcing
{
    /// <summary>
    /// 发布寻货需求命令（v1.35.0）：个人用户发布求购询价单。
    /// 额度模型：免费 N 条/天（SystemConfig）→ 超出花积分（规则表单价）→ 硬上限拒绝；
    /// 扣积分与建单同命令内完成（同库同 UnitOfWork 窗口，这是寻货不拆微服务的核心原因）
    /// </summary>
    /// <param name="UserId">发布人</param>
    /// <param name="PartNumber">型号文本（必填）</param>
    /// <param name="BearingId">关联平台轴承（搜索选中时有值）</param>
    /// <param name="Brand">期望品牌</param>
    /// <param name="Quantity">数量描述</param>
    /// <param name="ExpectedDelivery">交期描述</param>
    /// <param name="Region">地区</param>
    /// <param name="Description">补充说明</param>
    /// <param name="UsePoints">免费额度用完后确认花积分</param>
    public record PublishDemandCommand(
        Guid UserId, string PartNumber, Guid? BearingId, string? Brand, string? Quantity,
        string? ExpectedDelivery, string? Region, string? Description, bool UsePoints) : IRequest<Guid>;

    /// <summary>
    /// 发布寻货处理器
    /// </summary>
    public class PublishDemandCommandHandler : IRequestHandler<PublishDemandCommand, Guid>
    {
        private readonly ISourcingDemandRepository _demandRepository;
        private readonly ISystemConfigRepository _configRepository;
        private readonly IPointGrantRuleRepository _ruleRepository;
        private readonly IPointsService _pointsService;

        /// <summary>
        /// 构造：需求仓储 + 配置/规则仓储 + 积分服务（加量扣分）
        /// </summary>
        public PublishDemandCommandHandler(
            ISourcingDemandRepository demandRepository,
            ISystemConfigRepository configRepository,
            IPointGrantRuleRepository ruleRepository,
            IPointsService pointsService)
        {
            _demandRepository = demandRepository;
            _configRepository = configRepository;
            _ruleRepository = ruleRepository;
            _pointsService = pointsService;
        }

        /// <inheritdoc/>
        public async Task<Guid> Handle(PublishDemandCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.PartNumber))
                throw new InvalidOperationException("请填写寻货型号");
            if (request.PartNumber.Trim().Length > 100)
                throw new InvalidOperationException("型号过长（限 100 字）");

            // 额度评估：今日已发数（含取消单，防发了删删了发绕额度）
            var today = await _demandRepository.CountPublishedTodayAsync(request.UserId, cancellationToken);
            var freeLimit = await SourcingConfigReader.GetIntAsync(_configRepository, "Sourcing.FreePublishPerDay", 3);
            var rule = await _ruleRepository.GetEnabledByTypeAsync("sourcing_publish_bonus", cancellationToken);
            var hardLimit = rule?.DailyLimit ?? 10;
            var price = rule?.Amount ?? 20;
            var (decision, cost) = SourcingQuotaHelper.Evaluate(today, freeLimit, hardLimit, request.UsePoints, price);

            switch (decision)
            {
                case QuotaDecision.Rejected:
                    throw new InvalidOperationException($"今日发布已达上限（{hardLimit} 条），明天请早");
                case QuotaDecision.NeedPoints:
                    // 前端据此弹"花 X 积分发布"确认，带 UsePoints=true 重提交
                    throw new InvalidOperationException($"NEED_POINTS:{cost}");
            }

            // 超出免费额度：扣积分（余额不足由 DeductAsync 抛出转 400；扣分与建单同事务窗口）
            if (cost > 0)
            {
                await _pointsService.DeductAsync(request.UserId, "sourcing_publish_bonus", cost,
                    null, "寻货发布积分加量", cancellationToken);
            }

            var demand = SourcingDemand.Create(request.UserId, request.PartNumber, request.BearingId,
                request.Brand, request.Quantity, request.ExpectedDelivery, request.Region, request.Description);
            await _demandRepository.AddAsync(demand, cancellationToken);
            return demand.Id;
        }
    }
}
