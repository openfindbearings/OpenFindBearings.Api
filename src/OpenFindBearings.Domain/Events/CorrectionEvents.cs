using MediatR;

namespace OpenFindBearings.Domain.Events
{
    /// <summary>
    /// 纠错审核完成事件（v2.14.0）：管理员采纳或驳回纠错后触发。
    /// 订阅者：① 站内信通知提交人审核结果；② （预留）积分奖励——采纳时给提交人加积分，
    /// 积分体系（P8 商城）落地后新增订阅者挂接即可，本事件即扩展点，不改审批链路。
    /// </summary>
    public class CorrectionProcessedEvent : INotification
    {
        /// <summary>纠错请求ID</summary>
        public Guid CorrectionId { get; }
        /// <summary>纠错目标类型（Bearing/Merchant）</summary>
        public string TargetType { get; }
        /// <summary>目标实体ID（积分奖励等订阅者定位业务对象用）</summary>
        public Guid TargetId { get; }
        /// <summary>提交人用户ID（通知与积分发放对象）</summary>
        public Guid SubmittedBy { get; }
        /// <summary>被纠错的字段名</summary>
        public string FieldName { get; }
        /// <summary>是否被采纳</summary>
        public bool Approved { get; }
        /// <summary>审核意见（驳回时必填，通知正文引用）</summary>
        public string? ReviewComment { get; }
        /// <summary>事件发生时间（UTC）</summary>
        public DateTime OccurredOn { get; }

        public CorrectionProcessedEvent(Guid correctionId, string targetType, Guid targetId,
            Guid submittedBy, string fieldName, bool approved, string? reviewComment)
        {
            CorrectionId = correctionId;
            TargetType = targetType;
            TargetId = targetId;
            SubmittedBy = submittedBy;
            FieldName = fieldName;
            Approved = approved;
            ReviewComment = reviewComment;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
