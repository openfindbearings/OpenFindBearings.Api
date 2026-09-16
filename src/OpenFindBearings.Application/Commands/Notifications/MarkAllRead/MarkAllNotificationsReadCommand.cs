using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Notifications.MarkAllRead
{
    /// <summary>
    /// 全部站内信标记已读命令
    /// </summary>
    public record MarkAllNotificationsReadCommand : IRequest<int>, ICommand
    {
        /// <summary>操作人（收件人）</summary>
        public Guid UserId { get; init; }
    }

    /// <summary>
    /// 全部已读处理器：ExecuteUpdate 单 SQL 批量更新（仓储内即时执行，无需管道提交）
    /// </summary>
    public class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, int>
    {
        private readonly INotificationRepository _notificationRepository;

        public MarkAllNotificationsReadCommandHandler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<int> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
        {
            return await _notificationRepository.MarkAllReadAsync(request.UserId, cancellationToken);
        }
    }
}
