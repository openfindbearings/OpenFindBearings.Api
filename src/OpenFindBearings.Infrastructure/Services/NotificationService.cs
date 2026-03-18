using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Common.Interfaces;

namespace OpenFindBearings.Infrastructure.Services
{
    /// <summary>
    /// 通知服务实现
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger;
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
            // TODO: 实现发送给特定用户的通知
            _logger.LogInformation("发送给用户 {UserId} 通知: {Title} - {Message}", userId, title, message);
            await Task.CompletedTask;
        }
    }
}
