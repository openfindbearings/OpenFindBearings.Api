using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Notifications.ClearReadNotifications
{
    /// <summary>
    /// 清空已读站内信命令（批量删除本人所有已读消息，未读保留）。
    /// 改动说明（v2.12.0）：消息中心"清空已读"批量出口的后端支撑。
    /// </summary>
    public record ClearReadNotificationsCommand : IRequest<int>, ICommand
    {
        /// <summary>操作人（收件人）</summary>
        public Guid UserId { get; init; }
    }

    /// <summary>
    /// 清空处理器：ExecuteDelete 单 SQL 直删，返回删除行数供前端提示
    /// </summary>
    public class ClearReadNotificationsCommandHandler : IRequestHandler<ClearReadNotificationsCommand, int>
    {
        private readonly INotificationRepository _notificationRepository;

        public ClearReadNotificationsCommandHandler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<int> Handle(ClearReadNotificationsCommand request, CancellationToken cancellationToken)
        {
            return await _notificationRepository.DeleteReadAsync(request.UserId, cancellationToken);
        }
    }
}
