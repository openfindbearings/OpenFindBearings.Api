using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Application.Shared.Interfaces;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Infrastructure.Services
{
    /// <summary>
    /// 通知服务实现：站内信落库（Notifications 表）；管理员外部通知通道仍为日志占位
    /// 改动说明：此前 SendToUserAsync 仅记日志（TODO 占位），现升级为真实站内信写库；
    ///   事件订阅发生在 UnitOfWorkBehavior 提交之后，仓储 Add 只标记不提交，
    ///   故本服务在 Add 后显式 SaveChangesAsync 独立落库（通知写失败不回滚已成功的业务命令）
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;

        public NotificationService(
            ILogger<NotificationService> logger,
            INotificationRepository notificationRepository,
            IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task SendToAdminsAsync(string title, string message, CancellationToken cancellationToken = default)
        {
            // TODO: 实现发送给管理员的通知
            // 可以是邮件、短信、站内信、钉钉等
            _logger.LogInformation("发送给管理员通知: {Title} - {Message}", title, message);
            await Task.CompletedTask;
        }

        public async Task SendToUserAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default)
        {
            // 兼容旧调用：默认按系统类型落一条站内信
            await AddInAppAsync(userId, Notification.TypeSystem, title, message, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 写一条站内信并立即提交（订阅者时序在业务事务提交后，需独立 SaveChanges）
        /// </summary>
        public async Task AddInAppAsync(Guid userId, string type, string title, string body,
            string? bizType = null, Guid? bizId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                await _notificationRepository.AddAsync(
                    new Notification(userId, type, title, body, bizType, bizId), cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("站内信已送达: UserId={UserId}, Type={Type}, Title={Title}", userId, type, title);
            }
            catch (Exception ex)
            {
                // 通知失败只记录不影响业务链路（审核结果本身已落库）
                _logger.LogWarning(ex, "站内信写入失败: UserId={UserId}, Type={Type}", userId, type);
            }
        }
    }
}
