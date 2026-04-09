using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Events;

namespace OpenFindBearings.Application.EventHandlers
{
    /// <summary>
    /// 商家取消认证事件处理器
    /// </summary>
    public class MerchantUnverifiedEventHandler : INotificationHandler<MerchantUnverifiedEvent>
    {
        private readonly ILogger<MerchantUnverifiedEventHandler> _logger;

        public MerchantUnverifiedEventHandler(ILogger<MerchantUnverifiedEventHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Handle(MerchantUnverifiedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogWarning(
                "商家取消认证: 商家ID={MerchantId}, 商家名称={MerchantName}",
                notification.MerchantId,
                notification.MerchantName);
        }
    }
}
