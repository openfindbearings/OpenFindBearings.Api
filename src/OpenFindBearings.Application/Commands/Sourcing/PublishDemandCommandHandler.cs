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
    /// <param name="Visibility">v1.8.0 曝光范围：Public=公开（默认，进大厅 + 通知匹配商家）/ Invited=邀请（不进大厅，只推收件人）</param>
    /// <param name="InvitedMerchantIds">v1.8.0 邀请收件人勾选（空=全部匹配的认证在售商家；仅 Invited 时有意义）</param>
    /// <param name="IdempotencyKey">v1.8.2 客户端幂等键（可空）：同发布人同 key 的重复提交直接返回原单，不重复建单/扣分</param>
    public record PublishDemandCommand(
        Guid UserId, string PartNumber, Guid? BearingId, string? Brand, string? Quantity,
        string? ExpectedDelivery, string? Region, string? Description, bool UsePoints,
        Guid? MerchantId = null, SourcingVisibility Visibility = SourcingVisibility.Public,
        List<Guid>? InvitedMerchantIds = null, string? IdempotencyKey = null) : IRequest<PublishDemandResult>;

    /// <summary>
    /// 发布寻货结果（v1.8.1 口径纠正）：DemandId=新建寻货单 ID；
    /// RecipientMerchantCount=本次**收件人集合大小**（公开=在售匹配家数、邀请=勾选∩匹配家数、无匹配=0）。
    /// 改动说明（v1.8.1）：原字段名 NotifiedMerchantCount 暗示"实际收到推送的家数"，但实际站内信投递
    /// 还受"该商户是否已有在职管理员"影响（邀请已免除日限频），名实不符故更名，避免前端承诺过头。
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
        // 改动说明（v2.21.0 型号线索）：发布未收录型号时累积线索，供管理员判断是否建档
        private readonly IBearingLeadRepository _bearingLeadRepository;
        private readonly ILogger<PublishDemandCommandHandler> _logger;

        /// <summary>
        /// 单张邀请单的收件人上限（改动说明 v2.21.2）：邀请通知豁免每日限频，故用数量上限约束群发面
        /// </summary>
        private const int MaxInvitedMerchants = 50;

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
            IMerchantBearingRepository merchantBearingRepository,
            IBearingLeadRepository bearingLeadRepository,
            ILogger<PublishDemandCommandHandler> logger)
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
            _bearingLeadRepository = bearingLeadRepository;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<PublishDemandResult> Handle(PublishDemandCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.PartNumber))
                throw new InvalidOperationException("请填写寻货型号");
            if (request.PartNumber.Trim().Length > 100)
                throw new InvalidOperationException("型号过长（限 100 字）");

            // 改动说明（v1.57.3 自交易封堵）：发布人在职的全部商户都要排除，而不只是本次名义发布的商户——
            //   否则"个人名义发布"与"一人多商户"两种情形下，自家店铺会被匹配、被通知、可被邀请，形成自导自演闭环
            var myMerchantIds = (await _memberRepository.GetActiveByUserIdAsync(request.UserId, cancellationToken))
                .Select(m => m.MerchantId)
                .ToHashSet();
            if (request.MerchantId.HasValue)
                myMerchantIds.Add(request.MerchantId.Value);

            // 改动说明（v1.8.2 彻底幂等）：幂等键先于一切副作用检查——重放（网络重试/双击）直接返回原单，
            //   既不重复建单、也不重复扣分；键为空则维持原行为（不去重）
            var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim();
            if (idempotencyKey != null)
            {
                var replayed = await _demandRepository.GetByPublisherAndKeyAsync(
                    request.UserId, idempotencyKey, cancellationToken);
                if (replayed != null)
                {
                    // 回传的家数须与原响应一致：邀请取快照家数，公开按匹配口径现算（口径唯一来源仍是 SourcingMatcher）
                    var replayedCount = replayed.IsPrivate
                        ? replayed.InvitedMerchantIds.Count
                        : (await SourcingMatcher.GetMatchedMerchantIdsAsync(
                            _bearingRepository, _merchantBearingRepository, _merchantRepository,
                            replayed.BearingId, replayed.PartNumber, cancellationToken,
                            myMerchantIds)).Count;
                    return new PublishDemandResult(replayed.Id, replayedCount);
                }
            }

            // 改动说明（v1.8.1）：曝光范围必须落在枚举内——非法值会让单子"既进不了大厅、又不算邀请"（幽灵单），
            //   通知按公开文案发出却在大厅/matched 双向搜不到
            if (!Enum.IsDefined(request.Visibility))
                throw new InvalidOperationException("发布方式非法");
            // 改动说明（v1.8.1）：公开不接受收件人——客户端误把"公开+勾选"当邀请时显式报错，不静默忽略
            if (request.Visibility == SourcingVisibility.Public && request.InvitedMerchantIds is { Count: > 0 })
                throw new InvalidOperationException("公开无需指定收件商家，如需只邀指定商家请选择邀请");

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

            // 改动说明（v1.8.0 公开/邀请）：先解析"最终收件人集合"——它同时决定建单快照、推送事件与回传家数，
            //   必须在扣积分之前算定（邀请无人可邀时不得先烧额度/积分），且匹配口径只此一处（SourcingMatcher）
            // 改动说明（v1.8.1）：解析顺序提到额度评估之前——"无人可邀/勾选无效"必须在弹"花积分发布"确认框之前暴露；
            //   商户名义发布时排除发布商户自己（不自邀、不给自己发提醒）
            // 改动说明（v1.57.3 自交易封堵）：排除集合改为发布人在职的全部商户 + 名义发布商户，一人多商户同样不自邀
            var matched = await SourcingMatcher.GetMatchedMerchantIdsAsync(
                _bearingRepository, _merchantBearingRepository, _merchantRepository,
                request.BearingId, request.PartNumber, cancellationToken, myMerchantIds);

            IReadOnlyList<Guid> recipients;
            if (request.Visibility == SourcingVisibility.Invited)
            {
                if (request.InvitedMerchantIds is { Count: > 0 } picked)
                {
                    // 改动说明（v2.21.0 邀请放宽）：认证商家即可邀——不再要求"该型号有在售"，
                    //   以支持"请熟悉的商家帮忙找货"；逐家校验认证/启用/非自家，非法项点名报错，不再静默剔除
                    var distinctPicked = picked.Distinct().ToList();
                    // 改动说明（v2.21.2 邀请防滥用）：邀请单豁免每日通知限频（受邀必达），若收件人不设上限，
                    //   一次请求即可点名全平台认证商户，形成群发骚扰面；故对单次收件人数量设硬上限
                    if (distinctPicked.Count > MaxInvitedMerchants)
                        throw new InvalidOperationException($"一次最多邀请 {MaxInvitedMerchants} 家商家，请分批发布或改为公开");
                    var pickedMerchants = await _merchantRepository.GetByIdsAsync(distinctPicked, cancellationToken);
                    var invalid = new List<string>();
                    var valid = new List<Guid>();
                    foreach (var merchantId in distinctPicked)
                    {
                        var target = pickedMerchants.FirstOrDefault(m => m.Id == merchantId);
                        if (target == null)
                        {
                            invalid.Add("已不存在的商家");
                            continue;
                        }
                        if (myMerchantIds.Contains(merchantId))
                        {
                            invalid.Add($"{target.Name}（自己所在的商户）");
                            continue;
                        }
                        if (!target.IsVerified)
                        {
                            invalid.Add($"{target.Name}（未认证）");
                            continue;
                        }
                        if (!target.IsActive)
                        {
                            // 改动说明（v2.21.2 文案区分）：IsActive=false 是商户被软删（关店），与"停用"是两回事
                            invalid.Add($"{target.Name}（已关闭）");
                            continue;
                        }
                        // 改动说明（v2.21.2）：补商户状态校验——此前只查认证与软删标记，
                        //   已停用（Suspended）的认证商家仍会被邀请，但应答链路按状态拒绝，等于发出必死的邀请
                        if (target.Status != Domain.Enums.MerchantStatus.Active)
                        {
                            invalid.Add($"{target.Name}（已停用）");
                            continue;
                        }
                        valid.Add(merchantId);
                    }

                    if (invalid.Count > 0)
                        throw new InvalidOperationException($"以下商家不可邀请：{string.Join("、", invalid)}");

                    recipients = valid;
                }
                else
                {
                    // 勾选为空=邀全部匹配商家；改动说明（v2.21.0）：匹配集合已排除发布人在职的全部商户，
                    //   无匹配时提示改为公开或手动选择认证商家
                    if (matched.Count == 0)
                        throw new InvalidOperationException("当前没有可邀请的在售认证商家（自己的店铺不能邀请），请改为公开或手动选择认证商家");
                    recipients = matched.ToList();
                }
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
            // 改动说明（v1.8.0 公开/邀请）：事件携带最终收件人集合——订阅者直接按集合推送，
            //   不再自行匹配，从根上消除"回传家数"与"实际推送集合"两处口径漂移
            await _publisher.Publish(new SourcingDemandPublishedEvent(
                demand.Id, request.UserId, request.BearingId, demand.PartNumber, request.Brand,
                request.Visibility, recipients), cancellationToken);

            // 改动说明（v2.21.0 型号线索）：未关联平台型号且型号库确无此型号时记一次线索（只累加证据，不建型号）；
            //   线索写入失败只记日志，绝不影响已完成的发布结果
            if (request.BearingId == null)
            {
                try
                {
                    var partNumberKey = BearingLead.NormalizeKey(request.PartNumber);
                    if (!await _bearingLeadRepository.ExistsInLibraryAsync(partNumberKey, cancellationToken))
                    {
                        await _bearingLeadRepository.RecordAsync(
                            request.PartNumber, request.Brand, BearingLeadSource.Demand, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "记录型号线索失败: {PartNumber}", request.PartNumber);
                }
            }

            // 回传收件人集合大小，供前端区分"已通知 N 家认证商家"与"进入大厅等应答"
            return new PublishDemandResult(demand.Id, recipients.Count);
        }
    }
}
