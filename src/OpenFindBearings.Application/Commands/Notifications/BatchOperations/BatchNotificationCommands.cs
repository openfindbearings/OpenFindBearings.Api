using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Commands.Notifications.BatchOperations
{
    /// <summary>
    /// 批量标记通知已读命令（v2.12.0 列表多选）：收件人本人通知逐条标已读，
    /// 非本人/不存在静默跳过（不泄露存在性），单次 UnitOfWork 提交
    /// </summary>
    public record BatchMarkReadCommand : IRequest<int>, ICommand
    {
        /// <summary>通知 ID 集</summary>
        public List<Guid> Ids { get; init; } = new();

        /// <summary>操作人（收件人）</summary>
        public Guid UserId { get; init; }
    }

    /// <summary>批量标已读处理器</summary>
    public class BatchMarkReadCommandHandler : IRequestHandler<BatchMarkReadCommand, int>
    {
        private readonly INotificationRepository _notificationRepository;

        /// <summary>构造：通知仓储</summary>
        public BatchMarkReadCommandHandler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        /// <inheritdoc/>
        public async Task<int> Handle(BatchMarkReadCommand request, CancellationToken cancellationToken)
        {
            var count = 0;
            foreach (var id in request.Ids.Distinct())
            {
                var notification = await _notificationRepository.GetByIdForUserAsync(id, request.UserId, cancellationToken);
                if (notification == null) continue;
                notification.MarkRead();
                await _notificationRepository.UpdateAsync(notification, cancellationToken);
                count++;
            }
            return count;
        }
    }

    /// <summary>
    /// 批量删除通知命令（v2.12.0 列表多选）：收件人本人通知逐条删除，
    /// 非本人/不存在静默跳过，单次 UnitOfWork 提交
    /// </summary>
    public record BatchDeleteNotificationsCommand : IRequest<int>, ICommand
    {
        /// <summary>通知 ID 集</summary>
        public List<Guid> Ids { get; init; } = new();

        /// <summary>操作人（收件人）</summary>
        public Guid UserId { get; init; }
    }

    /// <summary>批量删除通知处理器</summary>
    public class BatchDeleteNotificationsCommandHandler : IRequestHandler<BatchDeleteNotificationsCommand, int>
    {
        private readonly INotificationRepository _notificationRepository;

        /// <summary>构造：通知仓储</summary>
        public BatchDeleteNotificationsCommandHandler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        /// <inheritdoc/>
        public async Task<int> Handle(BatchDeleteNotificationsCommand request, CancellationToken cancellationToken)
        {
            var count = 0;
            foreach (var id in request.Ids.Distinct())
            {
                // 仓储按 (id,userId) 复合归属删除，affected=0 即非本人/不存在——静默跳过
                var affected = await _notificationRepository.DeleteByIdForUserAsync(id, request.UserId, cancellationToken);
                if (affected > 0) count++;
            }
            return count;
        }
    }
}
