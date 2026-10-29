using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Events;

namespace OpenFindBearings.Application.EventHandlers
{
    /// <summary>
    /// 意见反馈回复通知处理器（v2.22.1）
    /// 管理员回复后向提交人写一条站内信，正文截取回复内容，便于用户在"我的反馈"之外的
    /// 消息中心也能直接看到处理结果。
    /// </summary>
    public class FeedbackRepliedNotificationHandler : INotificationHandler<FeedbackRepliedEvent>
    {
        private readonly ILogger<FeedbackRepliedNotificationHandler> _logger;
        private readonly INotificationService _notificationService;

        /// <summary>
        /// 构造：注入日志与站内信服务
        /// </summary>
        public FeedbackRepliedNotificationHandler(
            ILogger<FeedbackRepliedNotificationHandler> logger,
            INotificationService notificationService)
        {
            _logger = logger;
            _notificationService = notificationService;
        }

        /// <summary>
        /// 处理回复事件：给提交人发站内信
        /// </summary>
        public async Task Handle(FeedbackRepliedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("意见反馈已回复通知: FeedbackId={FeedbackId}, User={UserId}",
                notification.FeedbackId, notification.UserId);

            // 站内信正文控制在 200 字内，超长回复截断（完整内容在"我的反馈"查看）
            var reply = notification.Reply.Length > 200
                ? notification.Reply[..200] + "..."
                : notification.Reply;

            await _notificationService.AddInAppAsync(
                notification.UserId,
                Notification.TypeFeedbackReplied,
                "您的意见反馈已回复",
                $"平台回复：{reply}",
                Notification.BizFeedback,
                notification.FeedbackId,
                cancellationToken);
        }
    }
}