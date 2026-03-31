using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Interfaces;
using OpenFindBearings.Domain.Events;

namespace OpenFindBearings.Application.Features.Bearings.EventHandlers
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

            // TODO: 通知商家审核拒绝及原因
        }
    }
}
