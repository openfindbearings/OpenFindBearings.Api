using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 撮合会话（v2.18.0 T1 聚焦式会话）：绑定"寻货单 × 商户"的私域对话容器。
    /// 从单据/应答进入，双方各一名身份方（需求方方/商家方），不设机器人、不排队；
    /// 单据关闭后会话只读保留（历史可查，不可再发）。未读用双方各自的 LastReadAt 计算，
    /// 不给 Notification 加分类列（消息中心两类由本表与 Notification 并列构成）
    /// </summary>
    public class SourcingConversation : BaseEntity
    {
        /// <summary>寻货单标识（会话锚点之一）</summary>
        public Guid DemandId { get; private set; }

        /// <summary>商家标识（会话锚点之一）</summary>
        public Guid MerchantId { get; private set; }

        /// <summary>需求方用户标识（权限判定用：本会话归属的需求方本人）</summary>
        public Guid PublisherUserId { get; private set; }

        /// <summary>需求方商户标识（商户名义发布时有值；其成员同属需求方方）</summary>
        public Guid? PublisherMerchantId { get; private set; }

        /// <summary>需求方方最近已读时刻（UTC；null=从未读）</summary>
        public DateTime? PublisherLastReadAt { get; private set; }

        /// <summary>商家方最近已读时刻（UTC；null=从未读）</summary>
        public DateTime? MerchantLastReadAt { get; private set; }

        /// <summary>最近一条消息时刻（UTC；列表排序与"最后活跃"展示）</summary>
        public DateTime? LastMessageAt { get; private set; }

        /// <summary>最近一条消息摘要（截断展示，避免列表页拉全量消息体）</summary>
        public string? LastMessagePreview { get; private set; }

        /// <summary>供 EF Core 使用的无参构造</summary>
        private SourcingConversation() { }

        /// <summary>
        /// 创建会话：由寻货单与目标商户确定唯一身份（同单同商户全局唯一，由唯一索引兜底）
        /// </summary>
        /// <param name="demandId">寻货单标识</param>
        /// <param name="merchantId">商家标识</param>
        /// <param name="publisherUserId">需求方用户标识</param>
        /// <param name="publisherMerchantId">需求方商户标识（可为空）</param>
        public SourcingConversation(Guid demandId, Guid merchantId, Guid publisherUserId, Guid? publisherMerchantId)
        {
            DemandId = demandId;
            MerchantId = merchantId;
            PublisherUserId = publisherUserId;
            PublisherMerchantId = publisherMerchantId;
        }

        /// <summary>
        /// 记录一条新消息：刷新最后活跃时刻与摘要（摘要截断 60 字，防止列表页过长）
        /// </summary>
        /// <param name="preview">消息摘要</param>
        /// <param name="atUtc">消息时刻（UTC）</param>
        public void Touch(string preview, DateTime atUtc)
        {
            LastMessageAt = atUtc;
            LastMessagePreview = preview.Length <= 60 ? preview : preview[..60];
            UpdateTimestamp();
        }

        /// <summary>
        /// 标记某方已读（读接口调用）：把该方的已读水位推到当前时刻
        /// </summary>
        /// <param name="side">已读方</param>
        /// <param name="atUtc">已读时刻（UTC）</param>
        public void MarkRead(SourcingSenderSide side, DateTime atUtc)
        {
            if (side == SourcingSenderSide.Publisher)
                PublisherLastReadAt = atUtc;
            else
                MerchantLastReadAt = atUtc;
            UpdateTimestamp();
        }
    }
}
