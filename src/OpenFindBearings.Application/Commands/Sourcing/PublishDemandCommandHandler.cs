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
    /// <param name="MerchantId">v2.12.0 商户名义发布：发布商户（须为在职成员）；null=个人名义</param>
    public record PublishDemandCommand(
        Guid UserId, string PartNumber, Guid? BearingId, string? Brand, string? Quantity,
        string? ExpectedDelivery, string? Region, string? Description, bool UsePoints,
        Guid? MerchantId = null) : IRequest<Guid>;

    /// <summary>
    /// 发布寻货处理器
    /// </summary>
    public class PublishDemandCommandHandler : IRequestHandler<PublishDemandCommand, Guid>
    {
        private readonly ISourcingDemandRepository _demandRepository;
        private readonly ISystemConfigRepository _configRepository;
        private readonly IPointGrantRuleRepository _ruleRepository;
        private readonly IPointsService _pointsService;
        // v2.5.0 商家经济：发布额度按最佳商家等级加成
        private readonly IMerchantGradeService _merchantGrades;
        // v2.6.0 新手旅程：发布计数喂成就引擎
        private readonly IAchievementService _achievements;
        // v2.12.0 商户名义发布：成员校验 + 商户名快照
        private readonly IMerchantMemberRepository _memberRepository;
        private readonly IMerchantRepository _merchantRepository;

        /// <summary>
        /// 构造：需求仓储 + 配置/规则仓储 + 积分服务（加量扣分）+ 商家等级（额度 buff）+ 成就引擎（发布计数）
        /// + 成员/商户仓储（v2.12.0 商户名义发布校验）
        /// </summary>
        public PublishDemandCommandHandler(
            ISourcingDemandRepository demandRepository,
            ISystemConfigRepository configRepository,
            IPointGrantRuleRepository ruleRepository,
            IPointsService pointsService,
            IMerchantGradeService grades,
            IAchievementService achievements,
            IMerchantMemberRepository memberRepository,
            IMerchantRepository merchantRepository)
        {
            _demandRepository = demandRepository;
            _configRepository = configRepository;
            _ruleRepository = ruleRepository;
            _pointsService = pointsService;
            _merchantGrades = grades;
            _achievements = achievements;
            _memberRepository = memberRepository;
            _merchantRepository = merchantRepository;
        }

        /// <inheritdoc/>
        public async Task<Guid> Handle(PublishDemandCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.PartNumber))
                throw new InvalidOperationException("请填写寻货型号");
            if (request.PartNumber.Trim().Length > 100)
                throw new InvalidOperationException("型号过长（限 100 字）");

            // v2.12.0 商户名义发布：先验身份再动额度/积分（身份非法不得消耗额度）
            Merchant? publishMerchant = null;
            if (request.MerchantId.HasValue)
            {
                var member = await _memberRepository.GetActiveByUserAndMerchantAsync(
                    request.UserId, request.MerchantId.Value, cancellationToken)
                    ?? throw new InvalidOperationException("您不是该商户的在职成员，无法以其名义发布");
                publishMerchant = await _merchantRepository.GetByIdAsync(request.MerchantId.Value, cancellationToken)
                    ?? throw new InvalidOperationException("商户不存在");
            }

            // 额度评估：今日已发数（含取消单，防发了删删了发绕额度）
            var today = await _demandRepository.CountPublishedTodayAsync(request.UserId, cancellationToken);
            var freeLimit = await SourcingConfigReader.GetIntAsync(_configRepository, "Sourcing.FreePublishPerDay", 3);
            // 改动说明（v2.5.0 商家经济）：Lv2+ 认证商家成员发布免费额度 +1（最佳商家口径）
            var best = await _merchantGrades.GetBestForUserAsync(request.UserId, cancellationToken);
            freeLimit += MerchantBuffs.PublishQuotaBonus(best?.Grade ?? 0);
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
                request.Brand, request.Quantity, request.ExpectedDelivery, request.Region, request.Description,
                publishMerchant?.Id, publishMerchant?.Name);
            await _demandRepository.AddAsync(demand, cancellationToken);
            // 改动说明（v2.6.0 新手旅程）：发布成功喂 sourcing_publish_total 计数（"旗开得胜"勋章）；
            // 成就失败绝不反噬发布主流程（需求行仍由 UnitOfWork 管道提交），吞异常继续
            try
            {
                await _achievements.IncrementAsync(Domain.Entities.AchievementScope.Personal,
                    request.UserId, "sourcing_publish_total", 1, cancellationToken);
            }
            catch { /* 成就旁路，吞 */ }
            return demand.Id;
        }
    }
}
