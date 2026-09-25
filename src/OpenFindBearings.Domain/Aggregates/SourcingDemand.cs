using OpenFindBearings.Domain.Abstractions;

namespace OpenFindBearings.Domain.Aggregates
{
    /// <summary>
    /// 寻货需求聚合根（v1.35.0）：个人用户发布的求购询价单（RFQ 模型，对标 1688 询价单）。
    /// 零内联依赖原则：只引用外部聚合的 Id（PublisherUserId/BearingId/应答挂 MerchantId），
    /// 不建导航属性——保持"将来可整体拆出"的干净边界；奖励/通知一律走领域事件
    /// </summary>
    public class SourcingDemand : BaseEntity
    {
        /// <summary>状态：进行中（可被应答）</summary>
        public const int StatusPublished = 1;
        /// <summary>状态：已选定关闭（发布人确认了某条应答，双方解锁联系方式）</summary>
        public const int StatusClosed = 2;
        /// <summary>状态：已过期（发布后 14 天无人确认，读时惰性判定）</summary>
        public const int StatusExpired = 3;
        /// <summary>状态：发布人主动取消</summary>
        public const int StatusCancelled = 4;
        /// <summary>状态：平台下架（违规内容，Admin 治理）</summary>
        public const int StatusTakenDown = 5;

        /// <summary>有效期天数（发布后自动过期）</summary>
        public const int ValidDays = 14;

        /// <summary>发布人用户 ID（个人账户，非商户）</summary>
        public Guid PublisherUserId { get; private set; }

        /// <summary>关联平台轴承 ID（型号搜索选中时有值；长尾型号自由文本时为 null）</summary>
        public Guid? BearingId { get; private set; }

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

        /// <summary>状态（Status* 常量）</summary>
        public int Status { get; private set; } = StatusPublished;

        /// <summary>过期时刻（CreatedAt + ValidDays，读时惰性判定过期）</summary>
        public DateTime ExpiryAt { get; private set; }

        /// <summary>关闭时刻（选定/取消/下架时写入）</summary>
        public DateTime? ClosedAt { get; private set; }

        /// <summary>被选定的应答 ID（StatusClosed 时有值，联系方式解锁的锚点）</summary>
        public Guid? SelectedResponseId { get; private set; }

        /// <summary>应答计数（列表展示用冗余列，应答时 +1；与子表行数的微小漂移可接受，避免列表 COUNT JOIN）</summary>
        public int ResponseCount { get; private set; }

        /// <summary>EF 专用无参构造</summary>
        protected SourcingDemand() { }

        /// <summary>
        /// 工厂：创建寻货需求（进行中，有效期 14 天）
        /// </summary>
        /// <param name="publisherUserId">发布人</param>
        /// <param name="partNumber">型号文本</param>
        /// <param name="bearingId">关联平台轴承（可空）</param>
        /// <param name="brand">期望品牌</param>
        /// <param name="quantity">数量描述</param>
        /// <param name="expectedDelivery">交期描述</param>
        /// <param name="region">地区</param>
        /// <param name="description">补充说明</param>
        public static SourcingDemand Create(Guid publisherUserId, string partNumber, Guid? bearingId,
            string? brand, string? quantity, string? expectedDelivery, string? region, string? description)
        {
            return new SourcingDemand
            {
                PublisherUserId = publisherUserId,
                PartNumber = partNumber.Trim(),
                BearingId = bearingId,
                Brand = Truncate(brand, 50),
                Quantity = Truncate(quantity, 30),
                ExpectedDelivery = Truncate(expectedDelivery, 30),
                Region = Truncate(region, 30),
                Description = Truncate(description, 500),
                Status = StatusPublished,
                ExpiryAt = DateTime.UtcNow.AddDays(ValidDays),
            };
        }

        /// <summary>是否进行中（可被应答/可被取消）</summary>
        public bool IsOpen => Status == StatusPublished && DateTime.UtcNow < ExpiryAt;

        /// <summary>惰性过期判定：进行中但已过有效期则转过期态（读路径触发，无需定时任务）</summary>
        /// <returns>本次是否发生了状态流转</returns>
        public bool TryExpire()
        {
            if (Status != StatusPublished || DateTime.UtcNow < ExpiryAt)
                return false;
            Status = StatusExpired;
            ClosedAt = DateTime.UtcNow;
            return true;
        }

        /// <summary>发布人选定某条应答关闭（记录选定项，触发双方联系方式解锁）</summary>
        /// <param name="responseId">被选定的应答 ID</param>
        public void Select(Guid responseId)
        {
            Status = StatusClosed;
            SelectedResponseId = responseId;
            ClosedAt = DateTime.UtcNow;
        }

        /// <summary>发布人主动取消</summary>
        public void Cancel()
        {
            Status = StatusCancelled;
            ClosedAt = DateTime.UtcNow;
        }

        /// <summary>平台下架（违规治理）</summary>
        public void TakeDown()
        {
            Status = StatusTakenDown;
            ClosedAt = DateTime.UtcNow;
        }

        /// <summary>应答数+1（新应答落库时同步维护冗余列）</summary>
        public void IncrementResponseCount() => ResponseCount++;

        /// <summary>
        /// 空值安全截断（超长直接截断，防脏数据入库；null 原样返回）
        /// </summary>
        private static string? Truncate(string? value, int max)
            => string.IsNullOrWhiteSpace(value) ? null
                : value.Trim().Length <= max ? value.Trim() : value.Trim()[..max];
    }
}
