using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Notifications.MarkNotificationRead
{
    /// <summary>
    /// 标记单条站内信已读命令
    /// </summary>
    public record MarkNotificationReadCommand : IRequest<bool>, ICommand
    {
        /// <summary>通知 ID</summary>
        public Guid Id { get; init; }

        /// <summary>操作人（收件人，越权防护：非本人通知返回 404）</summary>
        public Guid UserId { get; init; }
    }

    /// <summary>
    /// 标记单条已读处理器（仓储只标记，提交由 UnitOfWork 管道完成）
    /// </summary>
    public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, bool>
    {
        private readonly INotificationRepository _notificationRepository;

        public MarkNotificationReadCommandHandler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<bool> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
        {
            var notification = await _notificationRepository.GetByIdForUserAsync(request.Id, request.UserId, cancellationToken);
            if (notification == null)
            {
                throw new KeyNotFoundException("通知不存在");
            }

            notification.MarkRead();
            await _notificationRepository.UpdateAsync(notification, cancellationToken);
            return true;
        }
    }
}
