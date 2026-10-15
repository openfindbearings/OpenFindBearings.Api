using MediatR;

namespace OpenFindBearings.Domain.Events
{
    /// <summary>
    /// 寻货收到新应答事件（v1.35.0）：商户应答成功后发布，订阅者通知发布人
    /// </summary>
    public class SourcingRespondedEvent : INotification
    {
        /// <summary>寻货需求 ID</summary>
        public Guid DemandId { get; }
        /// <summary>发布人（通知对象）</summary>
        public Guid PublisherUserId { get; }
        /// <summary>应答商户 ID</summary>
        public Guid MerchantId { get; }
        /// <summary>应答商户名（通知文案）</summary>
        public string MerchantName { get; }
        /// <summary>型号（通知文案）</summary>
        public string PartNumber { get; }
        /// <summary>发布商户 ID（可空；非空=商户名义发布，应答通知该商户全部在职成员而非仅经办人）</summary>
        public Guid? PublisherMerchantId { get; }

        /// <summary>
        /// 构造寻货新应答事件
        /// </summary>
        public SourcingRespondedEvent(Guid demandId, Guid publisherUserId, Guid merchantId, string merchantName, string partNumber,
            Guid? publisherMerchantId = null)
        {
            DemandId = demandId;
            PublisherUserId = publisherUserId;
            MerchantId = merchantId;
            MerchantName = merchantName;
            PartNumber = partNumber;
            PublisherMerchantId = publisherMerchantId;
        }
    }

    /// <summary>
    /// 寻货选定关闭事件（v1.35.0）：发布人确认某条应答后发布，
    /// 订阅者通知被选商户（含其余应答者需求已关闭）
    /// </summary>
    public class SourcingDemandClosedEvent : INotification
    {
        /// <summary>寻货需求 ID</summary>
        public Guid DemandId { get; }
        /// <summary>发布人</summary>
        public Guid PublisherUserId { get; }
        /// <summary>被选定的应答 ID</summary>
        public Guid SelectedResponseId { get; }
        /// <summary>被选商户 ID</summary>
        public Guid SelectedMerchantId { get; }
        /// <summary>被选商户名（通知文案）</summary>
        public string SelectedMerchantName { get; }
        /// <summary>型号（通知文案）</summary>
        public string PartNumber { get; }

        /// <summary>
        /// 构造寻货选定事件
        /// </summary>
        public SourcingDemandClosedEvent(Guid demandId, Guid publisherUserId, Guid selectedResponseId,
            Guid selectedMerchantId, string selectedMerchantName, string partNumber)
        {
            DemandId = demandId;
            PublisherUserId = publisherUserId;
            SelectedResponseId = selectedResponseId;
            SelectedMerchantId = selectedMerchantId;
            SelectedMerchantName = selectedMerchantName;
            PartNumber = partNumber;
        }
    }

    /// <summary>
    /// 寻货取消事件（v1.35.0）：发布人主动取消，订阅者通知全体应答商户
    /// </summary>
    public class SourcingDemandCancelledEvent : INotification
    {
        /// <summary>寻货需求 ID</summary>
        public Guid DemandId { get; }
        /// <summary>发布人</summary>
        public Guid PublisherUserId { get; }
        /// <summary>型号（通知文案）</summary>
        public string PartNumber { get; }

        /// <summary>
        /// 构造寻货取消事件
        /// </summary>
        public SourcingDemandCancelledEvent(Guid demandId, Guid publisherUserId, string partNumber)
        {
            DemandId = demandId;
            PublisherUserId = publisherUserId;
            PartNumber = partNumber;
        }
    }

    /// <summary>
    /// 寻货下架事件（v1.35.0）：Admin 治理下架违规单，订阅者通知发布人
    /// </summary>
    public class SourcingDemandTakenDownEvent : INotification
    {
        /// <summary>寻货需求 ID</summary>
        public Guid DemandId { get; }
        /// <summary>发布人（通知对象）</summary>
        public Guid PublisherUserId { get; }
        /// <summary>型号（通知文案）</summary>
        public string PartNumber { get; }
        /// <summary>下架原因（可选，透传给发布人）</summary>
        public string? Reason { get; }

        /// <summary>
        /// 构造寻货下架事件
        /// </summary>
        public SourcingDemandTakenDownEvent(Guid demandId, Guid publisherUserId, string partNumber, string? reason)
        {
            DemandId = demandId;
            PublisherUserId = publisherUserId;
            PartNumber = partNumber;
            Reason = reason;
        }
    }

    /// <summary>
    /// 寻货发布事件（v1.6.0 搜索寻货联动）：发布成功后由命令处理器发布，
    /// 订阅者按"在售本轴承的认证商家"匹配推送（广播），或仅推定向目标商户（定向单）。
    /// </summary>
    public class SourcingDemandPublishedEvent : INotification
    {
        /// <summary>寻货需求 ID</summary>
        public Guid DemandId { get; }
        /// <summary>发布人</summary>
        public Guid PublisherUserId { get; }
        /// <summary>关联平台轴承 ID（可空，无则无法精确匹配）</summary>
        public Guid? BearingId { get; }
        /// <summary>型号（通知文案 + 限频匹配片段）</summary>
        public string PartNumber { get; }
        /// <summary>期望品牌（通知文案，可空）</summary>
        public string? Brand { get; }
        /// <summary>定向目标商户 ID（可空；非空=定向单只推该商户）</summary>
        public Guid? TargetMerchantId { get; }

        /// <summary>
        /// 构造寻货发布事件
        /// </summary>
        public SourcingDemandPublishedEvent(Guid demandId, Guid publisherUserId, Guid? bearingId,
            string partNumber, string? brand, Guid? targetMerchantId)
        {
            DemandId = demandId;
            PublisherUserId = publisherUserId;
            BearingId = bearingId;
            PartNumber = partNumber;
            Brand = brand;
            TargetMerchantId = targetMerchantId;
        }
    }

    /// <summary>
    /// 撮合达成事件（v1.6.0 开闭源边界预留）：发布人选定应答（互给电话）时发布，
    /// 供闭源交易域订阅接续（下单/支付）。开源侧不含任何闭源实现，仅留此事件为衔接点。
    /// </summary>
    public class MatchCompletedEvent : INotification
    {
        /// <summary>寻货需求 ID</summary>
        public Guid DemandId { get; }
        /// <summary>发布人</summary>
        public Guid PublisherUserId { get; }
        /// <summary>被选定的商户 ID</summary>
        public Guid SelectedMerchantId { get; }
        /// <summary>型号</summary>
        public string PartNumber { get; }

        /// <summary>
        /// 构造撮合达成事件
        /// </summary>
        public MatchCompletedEvent(Guid demandId, Guid publisherUserId, Guid selectedMerchantId, string partNumber)
        {
            DemandId = demandId;
            PublisherUserId = publisherUserId;
            SelectedMerchantId = selectedMerchantId;
            PartNumber = partNumber;
        }
    }
}
