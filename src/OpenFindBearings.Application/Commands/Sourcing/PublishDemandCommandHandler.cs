using MediatR;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Events;
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
    /// <param name="Visibility">v1.8.0 曝光范围：Public=公开招标（默认，进大厅 + 通知匹配商家）/ Invited=邀标（不进大厅，只推收件人）</param>
    /// <param name="InvitedMerchantIds">v1.8.0 邀标收件人勾选（空=全部匹配的认证在售商家；仅 Invited 时有意义）</param>
    /// <param name="IdempotencyKey">v1.8.2 客户端幂等键（可空）：同发布人同 key 的重复提交直接返回原单，不重复建单/扣分</param>
    public record PublishDemandCommand(
        Guid UserId, string PartNumber, Guid? BearingId, string? Brand, string? Quantity,
        string? ExpectedDelivery, string? Region, string? Description, bool UsePoints,
        Guid? MerchantId = null, SourcingVisibility Visibility = SourcingVisibility.Public,
        List<Guid>? InvitedMerchantIds = null, string? IdempotencyKey = null) : IRequest<PublishDemandResult>;

    /// <summary>
    /// 发布寻货结果（v1.8.1 口径纠正）：DemandId=新建寻货单 ID；
    /// RecipientMerchantCount=本次**收件人集合大小**（公开招标=在售匹配家数、邀标=勾选∩匹配家数、无匹配=0）。
    /// 改动说明（v1.8.1）：原字段名 NotifiedMerchantCount 暗示"实际收到推送的家数"，但实际站内信投递
    /// 还受"该商户是否已有在职管理员"影响（邀标已免除日限频），名实不符故更名，避免前端承诺过头。
    /// </summary>
    /// <param name="DemandId">新建寻货单 ID</param>
    /// <param name="RecipientMerchantCount">收件人集合大小（认证商家家数）</param>
    public record PublishDemandResult(Guid DemandId, int RecipientMerchantCount);

    /// <summary>
    /// 发布寻货处理器
    /// </summary>
    public class PublishDemandCommandHandler : IRequestHandler<PublishDemandCommand, PublishDemandResult>
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
        // v1.6.0 搜索寻货联动：发布成功后发布事件，驱动"匹配认证商家"推送
        private readonly IPublisher _publisher;
        // 改动说明（v1.7.0 两条路径可见化）：回传"已匹配认证商家家数"，与推送订阅者共用 SourcingMatcher 口径
        private readonly IBearingRepository _bearingRepository;
        private readonly IMerchantBearingRepository _merchantBearingRepository;

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
            IMerchantRepository merchantRepository,
            IPublisher publisher,
            IBearingRepository bearingRepository,
            IMerchantBearingRepository merchantBearingRepository)
        {
            _demandRepository = demandRepository;
            _configRepository = configRepository;
            _ruleRepository = ruleRepository;
            _pointsService = pointsService;
            _merchantGrades = grades;
            _achievements = achievements;
            _memberRepository = memberRepository;
            _merchantRepository = merchantRepository;
            _publisher = publisher;
            _bearingRepository = bearingRepository;
            _merchantBearingRepository = merchantBearingRepository;
        }

        /// <inheritdoc/>
        public async Task<PublishDemandResult> Handle(PublishDemandCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.PartNumber))
                throw new InvalidOperationException("请填写寻货型号");
            if (request.PartNumber.Trim().Length > 100)
                throw new InvalidOperationException("型号过长（限 100 字）");

            // 改动说明（v1.8.2 彻底幂等）：幂等键先于一切副作用检查——重放（网络重试/双击）直接返回原单，
            //   既不重复建单、也不重复扣分；键为空则维持原行为（不去重）
            var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim();
            if (idempotencyKey != null)
            {
                var replayed = await _demandRepository.GetByPublisherAndKeyAsync(
                    request.UserId, idempotencyKey, cancellationToken);
                if (replayed != null)
                {
                    // 回传的家数须与原响应一致：邀标取快照家数，公开按匹配口径现算（口径唯一来源仍是 SourcingMatcher）
                    var replayedCount = replayed.IsPrivate
                        ? replayed.InvitedMerchantIds.Count
                        : (await SourcingMatcher.GetMatchedMerchantIdsAsync(
                            _bearingRepository, _merchantBearingRepository, _merchantRepository,
                            replayed.BearingId, replayed.PartNumber, cancellationToken,
                            replayed.PublisherMerchantId)).Count;
                    return new PublishDemandResult(replayed.Id, replayedCount);
                }
            }

            // 改动说明（v1.8.1）：曝光范围必须落在枚举内——非法值会让单子"既进不了大厅、又不算邀标"（幽灵单），
            //   通知按公开文案发出却在大厅/matched 双向搜不到
            if (!Enum.IsDefined(request.Visibility))
                throw new InvalidOperationException("发布方式非法");
            // 改动说明（v1.8.1）：公开招标不接受收件人——客户端误把"公开+勾选"当邀标时显式报错，不静默忽略
            if (request.Visibility == SourcingVisibility.Public && request.InvitedMerchantIds is { Count: > 0 })
                throw new InvalidOperationException("公开招标无需指定收件商家，如需只邀指定商家请选择邀标");

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

            // 改动说明（v1.8.0 公开招标/邀标）：先解析"最终收件人集合"——它同时决定建单快照、推送事件与回传家数，
            //   必须在扣积分之前算定（邀标无人可邀时不得先烧额度/积分），且匹配口径只此一处（SourcingMatcher）
            // 改动说明（v1.8.1）：解析顺序提到额度评估之前——"无人可邀/勾选无效"必须在弹"花积分发布"确认框之前暴露；
            //   商户名义发布时排除发布商户自己（不自邀、不给自己发提醒）
            var matched = await SourcingMatcher.GetMatchedMerchantIdsAsync(
                _bearingRepository, _merchantBearingRepository, _merchantRepository,
                request.BearingId, request.PartNumber, cancellationToken, publishMerchant?.Id);

            IReadOnlyList<Guid> recipients;
            if (request.Visibility == SourcingVisibility.Invited)
            {
                if (matched.Count == 0)
                    throw new InvalidOperationException("当前没有在售该型号的认证商家，无法邀标，请改为公开招标");
                // 勾选为空=邀全部匹配商家；非空=勾选 ∩ 匹配（拦截未认证/无货的勾选，守住"只找有货的认证商家"口径）
                recipients = request.InvitedMerchantIds is { Count: > 0 } picked
                    ? matched.Where(picked.Contains).ToList()
                    : matched.ToList();
                if (recipients.Count == 0)
                    throw new InvalidOperationException("所选商家均非该型号的在售认证商家，请重新选择");
            }
            else
            {
                recipients = matched.ToList();
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
                // 改动说明（v1.8.2 彻底幂等）：以客户端幂等键作扣分幂等键，同一键的重复扣分被积分流水拦截
                //   （键为空时退化为原行为：不幂等，仅依赖"重放先命中原单"的建单去重兜底）
                await _pointsService.DeductAsync(request.UserId, "sourcing_publish_bonus", cost,
                    idempotencyKey, "寻货发布积分加量", cancellationToken);
            }

            var demand = SourcingDemand.Create(request.UserId, request.PartNumber, request.BearingId,
                request.Brand, request.Quantity, request.ExpectedDelivery, request.Region, request.Description,
                publishMerchant?.Id, publishMerchant?.Name, request.Visibility, recipients, idempotencyKey);
            await _demandRepository.AddAsync(demand, cancellationToken);
            // 改动说明（v2.6.0 新手旅程）：发布成功喂 sourcing_publish_total 计数（"旗开得胜"勋章）；
            // 成就失败绝不反噬发布主流程（需求行仍由 UnitOfWork 管道提交），吞异常继续
            try
            {
                await _achievements.IncrementAsync(Domain.Entities.AchievementScope.Personal,
                    request.UserId, Domain.Entities.AchievementMetricKeys.SourcingPublishTotal, 1, cancellationToken);
            }
            catch { /* 成就旁路，吞 */ }
            // 改动说明（v1.8.0 公开招标/邀标）：事件携带最终收件人集合——订阅者直接按集合推送，
            //   不再自行匹配，从根上消除"回传家数"与"实际推送集合"两处口径漂移
            await _publisher.Publish(new SourcingDemandPublishedEvent(
                demand.Id, request.UserId, request.BearingId, demand.PartNumber, request.Brand,
                request.Visibility, recipients), cancellationToken);

            // 回传收件人集合大小，供前端区分"已通知 N 家认证商家"与"进入大厅等应答"
            return new PublishDemandResult(demand.Id, recipients.Count);
        }
    }
}
