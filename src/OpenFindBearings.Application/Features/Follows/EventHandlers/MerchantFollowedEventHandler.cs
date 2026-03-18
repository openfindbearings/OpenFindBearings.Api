using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Common.Constants;
using OpenFindBearings.Application.Common.Interfaces;
using OpenFindBearings.Domain.Events;

namespace OpenFindBearings.Application.Features.Follows.EventHandlers
{
    /// <summary>
    /// 商家被关注事件处理器
    /// </summary>
    public class MerchantFollowedEventHandler : INotificationHandler<MerchantFollowedEvent>
    {
        private readonly ILogger<MerchantFollowedEventHandler> _logger;
        private readonly ICacheService _cacheService;

        public MerchantFollowedEventHandler(
            ILogger<MerchantFollowedEventHandler> logger,
            ICacheService cacheService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        public async Task Handle(MerchantFollowedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "商家被关注: 用户ID={UserId}, 商家ID={MerchantId}, 时间={OccurredOn}",
                notification.UserId,
                notification.MerchantId,
                notification.OccurredOn);

            // 更新相关缓存
            //await _cacheService.RemoveAsync($"user_{notification.UserId}_follows", cancellationToken);
            //await _cacheService.RemoveAsync($"merchant_{notification.MerchantId}_followers", cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.GetUserFollowsKey(notification.UserId), cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.GetUserFollowIdsKey(notification.UserId), cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.GetMerchantFollowersKey(notification.MerchantId), cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.GetMerchantKey(notification.MerchantId), cancellationToken);
        }
    }
}
