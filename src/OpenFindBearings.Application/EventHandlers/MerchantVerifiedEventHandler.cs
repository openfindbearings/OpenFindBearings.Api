using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Interfaces;
using OpenFindBearings.Domain.Events;

namespace OpenFindBearings.Application.EventHandlers
{
    /// <summary>
    /// 商家认证通过事件处理器
    /// </summary>
    public class MerchantVerifiedEventHandler : INotificationHandler<MerchantVerifiedEvent>
    {
        private readonly ILogger<MerchantVerifiedEventHandler> _logger;
        private readonly INotificationService _notificationService;

        public MerchantVerifiedEventHandler(
            ILogger<MerchantVerifiedEventHandler> logger,
            INotificationService notificationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        public async Task Handle(MerchantVerifiedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "商家认证通过: 商家ID={MerchantId}, 商家名称={MerchantName}",
                notification.MerchantId,
                notification.MerchantName);

            try
            {
                await _notificationService.SendToAdminsAsync(
                    "商家认证通过",
                    $"商家 {notification.MerchantName} 已通过认证",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "通知商家认证通过失败: MerchantId={MerchantId}", notification.MerchantId);
            }
        }
    }
}
