using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Interfaces;
using OpenFindBearings.Application.Shared.Constants;
using OpenFindBearings.Domain.Events;

namespace OpenFindBearings.Application.Features.Favorites.EventHandlers
{
    /// <summary>
    /// 轴承被取消收藏事件处理器
    /// </summary>
    public class BearingUnfavoritedEventHandler : INotificationHandler<BearingUnfavoritedEvent>
    {
        private readonly ILogger<BearingUnfavoritedEventHandler> _logger;
        private readonly ICacheService _cacheService;

        public BearingUnfavoritedEventHandler(
            ILogger<BearingUnfavoritedEventHandler> logger,
            ICacheService cacheService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        public async Task Handle(BearingUnfavoritedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "轴承被取消收藏: 用户ID={UserId}, 轴承ID={BearingId}, 时间={OccurredOn}",
                notification.UserId,
                notification.BearingId,
                notification.OccurredOn);

            // 更新相关缓存
            //await _cacheService.RemoveAsync($"user_{notification.UserId}_favorites", cancellationToken);
            //await _cacheService.RemoveAsync($"bearing_{notification.BearingId}_favorited", cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.GetUserFavoritesKey(notification.UserId), cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.GetUserFavoriteIdsKey(notification.UserId), cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.GetBearingKey(notification.BearingId), cancellationToken);
        }
    }
}
