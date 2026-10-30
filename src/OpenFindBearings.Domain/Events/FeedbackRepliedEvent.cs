using MediatR;

namespace OpenFindBearings.Domain.Events
{
    /// <summary>
    /// 意见反馈已回复事件（v2.22.1）
    /// 管理员回复后触发，订阅者向提交人发站内信，打通"用户提交 → 平台回复 → 结果触达"闭环。
    /// 事件在业务事务提交后发布，回复落库失败不会误发通知。
    /// </summary>
    public class FeedbackRepliedEvent : INotification
    {
        /// <summary>反馈 ID</summary>
        public Guid FeedbackId { get; }

        /// <summary>提交人用户 ID（通知收件人）</summary>
        public Guid UserId { get; }

        /// <summary>管理员回复内容（通知正文引用）</summary>
        public string Reply { get; }

        /// <summary>事件发生时间（UTC）</summary>
        public DateTime OccurredOn { get; }

        /// <summary>
        /// 构造反馈已回复事件
        /// </summary>
        /// <param name="feedbackId">反馈 ID</param>
        /// <param name="userId">提交人用户 ID</param>
        /// <param name="reply">管理员回复内容</param>
        public FeedbackRepliedEvent(Guid feedbackId, Guid userId, string reply)
        {
            FeedbackId = feedbackId;
            UserId = userId;
            Reply = reply;
            OccurredOn = DateTime.UtcNow;
        }
    }
}