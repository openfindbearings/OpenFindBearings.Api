using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Interfaces;
using OpenFindBearings.Application.Shared.Constants;
using OpenFindBearings.Domain.Events;

namespace OpenFindBearings.Application.Features.Bearings.EventHandlers
{
    /// <summary>
    /// 轴承上架事件处理器
    /// </summary>
    public class BearingPutOnShelfEventHandler : INotificationHandler<BearingPutOnShelfEvent>
    {
        private readonly ILogger<BearingPutOnShelfEventHandler> _logger;
        private readonly ICacheService _cacheService;

        public BearingPutOnShelfEventHandler(
            ILogger<BearingPutOnShelfEventHandler> logger,
            ICacheService cacheService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        public async Task Handle(BearingPutOnShelfEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "轴承已上架: 商家ID={MerchantId}, 轴承ID={BearingId}, 关联ID={MerchantBearingId}",
                notification.MerchantId,
                notification.BearingId,
                notification.MerchantBearingId);

            // 清除相关缓存
            //await _cacheService.RemoveAsync($"merchant_{notification.MerchantId}_bearings", cancellationToken);
            //await _cacheService.RemoveAsync($"bearing_{notification.BearingId}_merchants", cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.GetMerchantProductsKey(notification.MerchantId), cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.GetBearingMerchantsKey(notification.BearingId), cancellationToken);
        }
    }
}
