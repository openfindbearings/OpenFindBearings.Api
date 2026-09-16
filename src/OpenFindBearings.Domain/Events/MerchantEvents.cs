using MediatR;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Events
{
    /// <summary>
    /// 商家认证通过事件
    /// 当管理员认证商家时触发
    /// </summary>
    public class MerchantVerifiedEvent : INotification
    {
        public Guid MerchantId { get; }
        public string MerchantName { get; }
        public DateTime OccurredOn { get; }

        public MerchantVerifiedEvent(Guid merchantId, string merchantName)
        {
            MerchantId = merchantId;
            MerchantName = merchantName;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 商家取消认证事件
    /// 当管理员取消商家认证时触发
    /// </summary>
    public class MerchantUnverifiedEvent : INotification
    {
        public Guid MerchantId { get; }
        public string MerchantName { get; }
        public DateTime OccurredOn { get; }

        public MerchantUnverifiedEvent(Guid merchantId, string merchantName)
        {
            MerchantId = merchantId;
            MerchantName = merchantName;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 商家等级变更事件
    /// 当商家等级变化时触发
    /// </summary>
    public class MerchantGradeChangedEvent : INotification
    {
        public Guid MerchantId { get; }
        public MerchantGrade OldGrade { get; }
        public MerchantGrade NewGrade { get; }
        public DateTime OccurredOn { get; }

        public MerchantGradeChangedEvent(Guid merchantId, MerchantGrade oldGrade, MerchantGrade newGrade)
        {
            MerchantId = merchantId;
            OldGrade = oldGrade;
            NewGrade = newGrade;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 商家入驻审核通过事件
    /// 改动说明：与 MerchantVerifiedEvent（资质认证）语义区分——本事件表示入驻申请审批生效，
    ///   站内信订阅者据此通知商户在职管理员
    /// </summary>
    public class MerchantApprovedEvent : INotification
    {
        public Guid MerchantId { get; }
        public string MerchantName { get; }
        public DateTime OccurredOn { get; }

        public MerchantApprovedEvent(Guid merchantId, string merchantName)
        {
            MerchantId = merchantId;
            MerchantName = merchantName;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 商家入驻审核拒绝事件：携带拒绝原因，站内信订阅者据此通知商户在职管理员
    /// </summary>
    public class MerchantRejectedEvent : INotification
    {
        public Guid MerchantId { get; }
        public string MerchantName { get; }
        public string Reason { get; }
        public DateTime OccurredOn { get; }

        public MerchantRejectedEvent(Guid merchantId, string merchantName, string reason)
        {
            MerchantId = merchantId;
            MerchantName = merchantName;
            Reason = reason;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 提名已被接受事件：由接受提名命令处理器发布，站内信订阅者据此通知发起人。
    /// 改动说明：不设"提名已发出"事件——被提名手机号到用户的解析在 Identity 侧，
    ///   API 的 User 实体无手机号，未注册用户站内信不可达，发出环节由邀请链接与向导待办承担触达。
    /// </summary>
    public class NominationAcceptedEvent : INotification
    {
        public Guid InitiatorUserId { get; }
        public Guid MerchantId { get; }
        public string MerchantName { get; }

        public NominationAcceptedEvent(Guid initiatorUserId, Guid merchantId, string merchantName)
        {
            InitiatorUserId = initiatorUserId;
            MerchantId = merchantId;
            MerchantName = merchantName;
        }
    }

    /// <summary>
    /// 商家被关注事件
    /// 当用户关注商家时触发
    /// </summary>
    public class MerchantFollowedEvent : INotification
    {
        public Guid UserId { get; }
        public Guid MerchantId { get; }
        public DateTime OccurredOn { get; }

        public MerchantFollowedEvent(Guid userId, Guid merchantId)
        {
            UserId = userId;
            MerchantId = merchantId;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 商家被取消关注事件
    /// 当用户取消关注商家时触发
    /// </summary>
    public class MerchantUnfollowedEvent : INotification
    {
        public Guid UserId { get; }
        public Guid MerchantId { get; }
        public DateTime OccurredOn { get; }

        public MerchantUnfollowedEvent(Guid userId, Guid merchantId)
        {
            UserId = userId;
            MerchantId = merchantId;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
