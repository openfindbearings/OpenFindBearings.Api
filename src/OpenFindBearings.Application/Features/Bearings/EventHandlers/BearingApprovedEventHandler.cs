using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Common.Interfaces;
using OpenFindBearings.Domain.Events;

namespace OpenFindBearings.Application.Features.Bearings.EventHandlers
{
    /// <summary>
    /// 轴承审核通过事件处理器
    /// </summary>
    public class BearingApprovedEventHandler : INotificationHandler<BearingApprovedEvent>
    {
        private readonly ILogger<BearingApprovedEventHandler> _logger;
        private readonly INotificationService _notificationService;

        public BearingApprovedEventHandler(
            ILogger<BearingApprovedEventHandler> logger,
            INotificationService notificationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        public async Task Handle(BearingApprovedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "轴承审核通过: 商家ID={MerchantId}, 轴承ID={BearingId}",
                notification.MerchantId,
                notification.BearingId);

            // TODO: 通知商家审核通过
        }
    }
}
