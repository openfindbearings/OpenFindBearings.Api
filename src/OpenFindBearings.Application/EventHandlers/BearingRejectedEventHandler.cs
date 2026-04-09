using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Interfaces;
using OpenFindBearings.Domain.Events;

namespace OpenFindBearings.Application.EventHandlers
{
    /// <summary>
    /// 轴承审核拒绝事件处理器
    /// </summary>
    public class BearingRejectedEventHandler : INotificationHandler<BearingRejectedEvent>
    {
        private readonly ILogger<BearingRejectedEventHandler> _logger;
        private readonly INotificationService _notificationService;

        public BearingRejectedEventHandler(
            ILogger<BearingRejectedEventHandler> logger,
            INotificationService notificationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        public async Task Handle(BearingRejectedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "轴承审核拒绝: 商家ID={MerchantId}, 轴承ID={BearingId}, 原因={RejectReason}",
                notification.MerchantId,
                notification.BearingId,
                notification.RejectReason);

            try
            {
                await _notificationService.SendToAdminsAsync(
                    "轴承审核未通过",
                    $"商家 {notification.MerchantId} 的轴承 {notification.BearingId} 未通过审核，原因：{notification.RejectReason}",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "通知商家审核拒绝失败: MerchantId={MerchantId}", notification.MerchantId);
            }
        }
    }
}
