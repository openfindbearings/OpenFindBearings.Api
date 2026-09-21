using OpenFindBearings.Domain.Abstractions;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 站内信通知实体：定向 1 对 1 写扩散（每个收件人一行），
    /// 由领域事件订阅者在命令事务提交后写入，供 Taro 消息中心与未读角标消费
    /// </summary>
    public class Notification : BaseEntity
    {
        /// <summary>通知类型：商户入驻审核通过</summary>
        public const string TypeMerchantApproved = "merchant_approved";
        /// <summary>通知类型：商户入驻审核拒绝</summary>
        public const string TypeMerchantRejected = "merchant_rejected";
        /// <summary>通知类型：收到管理员提名邀请</summary>
        public const string TypeNominationReceived = "nomination_received";
        /// <summary>通知类型：自己发出的提名已被接受</summary>
        public const string TypeNominationAccepted = "nomination_accepted";
        // 改动说明（v2.9.0 邀请确认制）：员工邀请两类站内信——发给被邀人（待确认）与发给发起人（已同意）
        public const string TypeStaffJoinInvited = "staff_join_invited";
        public const string TypeStaffJoinAccepted = "staff_join_accepted";
        /// <summary>通知类型：系统通用</summary>
        public const string TypeSystem = "system";

        /// <summary>业务类型：商户（前端据此跳转商户页）</summary>
        public const string BizMerchant = "merchant";
        /// <summary>业务类型：提名邀请（前端据此跳转入驻向导邀请相位）</summary>
        public const string BizNomination = "nomination";

        /// <summary>收件人用户 ID</summary>
        public Guid UserId { get; private set; }

        /// <summary>通知类型（Type* 常量）</summary>
        public string Type { get; private set; } = TypeSystem;

        /// <summary>标题</summary>
        public string Title { get; private set; } = string.Empty;

        /// <summary>正文</summary>
        public string Body { get; private set; } = string.Empty;

        /// <summary>业务类型（Biz* 常量，可空=无跳转）</summary>
        public string? BizType { get; private set; }

        /// <summary>业务对象 ID（跳转定位用）</summary>
        public Guid? BizId { get; private set; }

        /// <summary>是否已读</summary>
        public bool IsRead { get; private set; }

        /// <summary>已读时间（UTC）</summary>
        public DateTime? ReadAt { get; private set; }

        /// <summary>EF 构造函数</summary>
        protected Notification() { }

        /// <summary>
        /// 新建站内信：创建即未读，时间戳由基类置 UTC
        /// </summary>
        /// <param name="userId">收件人用户 ID</param>
        /// <param name="type">通知类型常量</param>
        /// <param name="title">标题</param>
        /// <param name="body">正文</param>
        /// <param name="bizType">业务类型（可空）</param>
        /// <param name="bizId">业务对象 ID（可空）</param>
        public Notification(Guid userId, string type, string title, string body,
            string? bizType = null, Guid? bizId = null)
        {
            UserId = userId;
            Type = type;
            Title = title;
            Body = body;
            BizType = bizType;
            BizId = bizId;
            IsRead = false;
        }

        /// <summary>
        /// 标记已读（幂等：已读则跳过）
        /// </summary>
        public void MarkRead()
        {
            if (IsRead) return;
            IsRead = true;
            ReadAt = DateTime.UtcNow;
            UpdateTimestamp();
        }
    }
}
