using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Notifications.DeleteNotification
{
    /// <summary>
    /// 删除单条站内信命令（收件人硬删自己的一条消息）。
    /// 改动说明（v2.12.0）：消息中心左滑删除入口的后端支撑。
    /// </summary>
    public record DeleteNotificationCommand : IRequest<bool>, ICommand
    {
        /// <summary>待删通知 Id</summary>
        public Guid Id { get; init; }

        /// <summary>操作人（收件人）</summary>
        public Guid UserId { get; init; }
    }

    /// <summary>
    /// 删除处理器：仓储按 Id+UserId 双条件直删，返回是否命中（未命中即不存在或非本人消息，端点转 404）
    /// </summary>
    public class DeleteNotificationCommandHandler : IRequestHandler<DeleteNotificationCommand, bool>
    {
        private readonly INotificationRepository _notificationRepository;

        public DeleteNotificationCommandHandler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<bool> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
        {
            var affected = await _notificationRepository.DeleteByIdForUserAsync(
                request.Id, request.UserId, cancellationToken);
            return affected > 0;
        }
    }
}
