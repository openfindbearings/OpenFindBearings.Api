using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Aggregates
{
    /// <summary>
    /// 寻货需求聚合根（v1.35.0）：个人用户发布的求购询价单（RFQ 模型，对标 1688 询价单）。
    /// 零内联依赖原则：只引用外部聚合的 Id（PublisherUserId/BearingId/应答挂 MerchantId），
    /// 不建导航属性——保持"将来可整体拆出"的干净边界；奖励/通知一律走领域事件
    /// </summary>
    public class SourcingDemand : BaseEntity
    {
        /// <summary>有效期天数（发布后自动过期）</summary>
        public const int ValidDays = 14;

        /// <summary>发布人用户 ID（经办人个人账号，管理权限归他）</summary>
        public Guid PublisherUserId { get; private set; }

        /// <summary>
        /// 发布商户 ID（可空）。改动说明（v2.12.0 商户名义发布）：发布人若是该商户在职成员，
        /// 可选以商户名义发布（默认）；null=个人名义。对外联系方式与徽章展示走商户口径
        /// </summary>
        public Guid? PublisherMerchantId { get; private set; }

        /// <summary>发布商户名称快照（列表/详情展示免 join；商户改名不回刷，保留发布时点事实）</summary>
        public string? PublisherMerchantName { get; private set; }

        /// <summary>
        /// 客户端发布幂等键（v1.8.2 彻底幂等）：同一发布人携带同一 key 的重复提交视为同一张单
        /// （唯一索引兜底，重放直接返回原单且不重复扣分）；空=客户端未提供，不做去重
        /// </summary>
        public string? IdempotencyKey { get; private set; }

        /// <summary>关联平台轴承 ID（型号搜索选中时有值；长尾型号自由文本时为 null）</summary>
        public Guid? BearingId { get; private set; }

        /// <summary>
        /// 曝光范围（v1.8.0 公开/邀请）：Public=进公开大厅 + 推送匹配的认证在售商家；
        /// Invited=不进大厅，只推给 InvitedMerchantIds 里的商家，且仅这些商家与发布人可见。
        /// </summary>
        public SourcingVisibility Visibility { get; private set; } = SourcingVisibility.Public;

        /// <summary>
        /// 邀请收件人商户 ID 快照（v1.8.0 邀请；仅 Visibility=Invited 时非空，不变量：Invited 等价于非空）。
        /// 改动说明：存 uuid[] 一列而非新建明细表——语义是发布时点快照（无外键、无逐收件人状态），
        /// 可见性与"为你匹配"查询只需 ANY 判定；整体替换式赋值，不做原地增删（防 EF 变更追踪漏检）。
        /// </summary>
        public List<Guid> InvitedMerchantIds { get; private set; } = new();

        /// <summary>是否邀请单（不进公开大厅，仅发布人与受邀商家可见）</summary>
        public bool IsPrivate => Visibility == SourcingVisibility.Invited;

        /// <summary>该商户是否在本次邀请收件人快照内（详情可见性判定用）</summary>
        /// <param name="merchantId">待判定商户 ID</param>
        public bool IsInvitedTo(Guid merchantId) => InvitedMerchantIds.Contains(merchantId);

        /// <summary>型号文本（必填，结构化比价与搜索的核心字段）</summary>
        public string PartNumber { get; private set; } = string.Empty;

        /// <summary>期望品牌（可选）</summary>
        public string? Brand { get; private set; }

        /// <summary>需求数量描述（可选，"50 套"/"1000 只"自由文本——B2B 单位多样不做死）</summary>
        public string? Quantity { get; private set; }

        /// <summary>期望交期描述（可选）</summary>
        public string? ExpectedDelivery { get; private set; }

        /// <summary>地区（可选，就近供货筛选）</summary>
        public string? Region { get; private set; }

        /// <summary>补充说明（可选）</summary>
        public string? Description { get; private set; }

        /// <summary>状态（SourcingDemandStatus 枚举，v2.15.0）</summary>
        public SourcingDemandStatus Status { get; private set; } = SourcingDemandStatus.Published;

        /// <summary>过期时刻（CreatedAt + ValidDays，读时惰性判定过期）</summary>
        public DateTime ExpiryAt { get; private set; }

        /// <summary>关闭时刻（选定/取消/下架时写入）</summary>
        public DateTime? ClosedAt { get; private set; }

        /// <summary>被选定的应答 ID（StatusClosed 时有值，联系方式解锁的锚点）</summary>
        public Guid? SelectedResponseId { get; private set; }

        /// <summary>
        /// 联系方式解锁时刻（UTC，v2.18.0 电话单向）：选定即解锁"需求方查看被选商家号"，
        /// 同时作为审计锚点（谁在何时获取了商家联系方式）；商家侧永不下发需求方号码
        /// </summary>
        public DateTime? ContactUnlockedAt { get; private set; }

        /// <summary>应答计数（列表展示用冗余列，应答时 +1；与子表行数的微小漂移可接受，避免列表 COUNT JOIN）</summary>
        public int ResponseCount { get; private set; }

        /// <summary>
        /// 置顶到期时刻（UTC，可空）。改动说明（v2.10.0 寻货置顶）：发布人兑换"寻货置顶卡"后
        /// 需求在公开列表排前并带角标；叠加规则=未过期时续加时长（与商品置顶卡一致）
        /// </summary>
        public DateTime? PinnedUntil { get; private set; }

        /// <summary>EF 专用无参构造</summary>
        protected SourcingDemand() { }

        /// <summary>
        /// 工厂：创建寻货需求（进行中，有效期 14 天）
        /// </summary>
        /// <param name="publisherUserId">发布人（经办人）</param>
        /// <param name="partNumber">型号文本</param>
        /// <param name="bearingId">关联平台轴承（可空）</param>
        /// <param name="brand">期望品牌</param>
        /// <param name="quantity">数量描述</param>
        /// <param name="expectedDelivery">交期描述</param>
        /// <param name="region">地区</param>
        /// <param name="description">补充说明</param>
        /// <param name="publisherMerchantId">发布商户（v2.12.0 商户名义发布，null=个人名义）</param>
        /// <param name="publisherMerchantName">商户名称快照（商户名义时必填）</param>
        /// <param name="visibility">曝光范围（v1.8.0 公开/邀请，默认公开）</param>
        /// <param name="invitedMerchantIds">邀请收件人（v1.8.0；仅邀请时有意义，调用方已按"在售匹配 ∩ 勾选"解析）</param>
        /// <param name="idempotencyKey">客户端幂等键（v1.8.2 彻底幂等，可空=不去重）</param>
        /// <exception cref="InvalidOperationException">邀请但收件人为空（调用方应先解析，此处兜底防脏数据）</exception>
        public static SourcingDemand Create(Guid publisherUserId, string partNumber, Guid? bearingId,
            string? brand, string? quantity, string? expectedDelivery, string? region, string? description,
            Guid? publisherMerchantId = null, string? publisherMerchantName = null,
            SourcingVisibility visibility = SourcingVisibility.Public, IReadOnlyList<Guid>? invitedMerchantIds = null,
            string? idempotencyKey = null)
        {
            // 不变量：邀请必须至少一位收件人，否则单子谁都看不见（发布处理器已按匹配口径解析，这里兜底）
            var invited = invitedMerchantIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
            if (visibility == SourcingVisibility.Invited && invited.Count == 0)
                throw new InvalidOperationException("邀请寻货至少需要一位在售该型号的认证商家");

            return new SourcingDemand
            {
                PublisherUserId = publisherUserId,
                PublisherMerchantId = publisherMerchantId,
                PublisherMerchantName = publisherMerchantId.HasValue ? Truncate(publisherMerchantName, 100) : null,
                PartNumber = partNumber.Trim(),
                BearingId = bearingId,
                // 改动说明（v1.8.2 彻底幂等）：发布幂等键随单落库，重复提交（同发布人同 key）由唯一索引拦截并返回原单
                IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim(),
                // 改动说明（v1.8.0 公开/邀请）：公开单不收件人（通知集合按在售匹配实时算），邀请单存收件人快照
                Visibility = visibility,
                InvitedMerchantIds = visibility == SourcingVisibility.Invited ? invited : new List<Guid>(),
                Brand = Truncate(brand, 50),
                Quantity = Truncate(quantity, 30),
                ExpectedDelivery = Truncate(expectedDelivery, 30),
                Region = Truncate(region, 30),
                Description = Truncate(description, 500),
                Status = SourcingDemandStatus.Published,
                ExpiryAt = DateTime.UtcNow.AddDays(ValidDays),
            };
        }

        /// <summary>是否商户名义发布（对外联系方式取商户公开电话的判定依据）</summary>
        public bool IsMerchantPublished => PublisherMerchantId.HasValue;

        /// <summary>是否进行中（可被应答/可被取消）</summary>
        public bool IsOpen => Status == SourcingDemandStatus.Published && DateTime.UtcNow < ExpiryAt;

        /// <summary>惰性过期判定：进行中但已过有效期则转过期态（读路径触发，无需定时任务）</summary>
        /// <returns>本次是否发生了状态流转</returns>
        public bool TryExpire()
        {
            if (Status != SourcingDemandStatus.Published || DateTime.UtcNow < ExpiryAt)
                return false;
            Status = SourcingDemandStatus.Expired;
            ClosedAt = DateTime.UtcNow;
            return true;
        }

        /// <summary>发布人选定某条应答关闭（记录选定项，解锁商家联系方式——v2.18.0 单向，需求方号码不下发商家）</summary>
        /// <param name="responseId">被选定的应答 ID</param>
        public void Select(Guid responseId)
        {
            Status = SourcingDemandStatus.Closed;
            SelectedResponseId = responseId;
            ClosedAt = DateTime.UtcNow;
            // 改动说明（v2.18.0 电话单向）：选定即解锁需求方查看商家联系方式，记录审计锚点
            ContactUnlockedAt = DateTime.UtcNow;
        }

        /// <summary>发布人主动取消</summary>
        public void Cancel()
        {
            Status = SourcingDemandStatus.Cancelled;
            ClosedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 转为公开（v1.8.0）：邀请单进行中时，由发布人放开为公开（需求进大厅，人人可见可应答），
        /// 收件人快照随之清空（不变量：邀请态等价于收件人非空）。
        /// 改动说明：邀请后无人应答时不必取消重建——原"定向单"只能取消再发，额度白白损耗。
        /// 改动说明（v1.8.2 转公开补发通知）：清空快照前调用方需自行留存原收件人集合（差集补发用），
        /// 本方法职责不变，仅做状态与快照变更。
        /// </summary>
        /// <exception cref="InvalidOperationException">已是公开单，或已非进行中（终态不可再放开）</exception>
        public void MakePublic()
        {
            if (Visibility == SourcingVisibility.Public)
                throw new InvalidOperationException("该寻货已是公开");
            if (!IsOpen)
                throw new InvalidOperationException("仅进行中的寻货可转为公开");
            Visibility = SourcingVisibility.Public;
            InvitedMerchantIds = new List<Guid>();
        }

        /// <summary>平台下架（违规治理）</summary>
        public void TakeDown()
        {
            Status = SourcingDemandStatus.TakenDown;
            ClosedAt = DateTime.UtcNow;
        }

        /// <summary>应答数+1（新应答落库时同步维护冗余列）</summary>
        public void IncrementResponseCount() => ResponseCount++;

        /// <summary>
        /// 应答数-1（v1.8.1 撤标计数维护）：撤回应答时同步递减冗余列，避免"撤了还显示有应答"的僵尸计数。
        /// 改动说明：该列是 matched/opportunities "尚无人应答"推荐的依据，漂移会让单子在商家聚合面消失；
        /// 带下界保护，绝不出现负数
        /// </summary>
        public void DecrementResponseCount()
        {
            if (ResponseCount > 0)
                ResponseCount--;
        }

        /// <summary>
        /// 置顶（v2.10.0 需求置顶卡履约）：有效期内续接（叠加到旧到期时刻之上），
        /// 已过期/从未置顶从当前时刻起算——与商品置顶同语义，防"过期卡叠满未来"
        /// </summary>
        /// <param name="hours">置顶时长（小时）</param>
        /// <param name="nowUtc">当前 UTC 时刻（调用方传入，保证与校验同源）</param>
        public void Pin(int hours, DateTime nowUtc)
        {
            var baseline = (PinnedUntil.HasValue && PinnedUntil.Value > nowUtc) ? PinnedUntil.Value : nowUtc;
            PinnedUntil = baseline.AddHours(hours);
        }

        /// <summary>是否处于置顶期（列表排序与角标判定）</summary>
        /// <param name="nowUtc">当前 UTC 时刻</param>
        public bool IsPinned(DateTime nowUtc) => PinnedUntil.HasValue && PinnedUntil.Value > nowUtc;

        /// <summary>
        /// 软删除标记（v2.12.0 列表删除）：仅发布方列表隐藏，应答方视图与数据保留。
        /// 改动说明：删除是"清单整理"不是"业务撤销"——进行中单必须先取消（通知应答者），
        /// 终态单才可删；守卫在 Delete() 内，服务端防绕过
        /// </summary>
        public bool IsDeleted { get; private set; }

        /// <summary>软删除（限终态单；进行中拒删——防跳过取消通知流程）</summary>
        /// <exception cref="InvalidOperationException">进行中单不可删</exception>
        public void Delete()
        {
            if (Status == SourcingDemandStatus.Published)
                throw new InvalidOperationException("进行中的寻货请先取消，再删除记录");
            IsDeleted = true;
        }

        /// <summary>
        /// 空值安全截断（超长直接截断，防脏数据入库；null 原样返回）
        /// </summary>
        private static string? Truncate(string? value, int max)
            => string.IsNullOrWhiteSpace(value) ? null
                : value.Trim().Length <= max ? value.Trim() : value.Trim()[..max];
    }
}
