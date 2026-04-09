using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Interfaces;
using OpenFindBearings.Application.Shared.Constants;
using OpenFindBearings.Domain.Events;

namespace OpenFindBearings.Application.EventHandlers
{
    /// <summary>
    /// 轴承更新事件处理器
    /// </summary>
    public class BearingUpdatedEventHandler : INotificationHandler<BearingUpdatedEvent>
    {
        private readonly ILogger<BearingUpdatedEventHandler> _logger;
        private readonly ICacheService _cacheService;

        public BearingUpdatedEventHandler(
            ILogger<BearingUpdatedEventHandler> logger,
            ICacheService cacheService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        public async Task Handle(BearingUpdatedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "处理轴承更新事件: 轴承ID={BearingId}, 型号={PartNumber}, 修改字段={ChangedFields}",
                notification.BearingId,
                notification.PartNumber,
                string.Join(", ", notification.ChangedFields));

            // 清除相关缓存
            //await _cacheService.RemoveAsync($"bearing_{notification.BearingId}", cancellationToken);
            //await _cacheService.RemoveAsync("recent_bearings", cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.GetBearingKey(notification.BearingId), cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.RecentBearings, cancellationToken);
            // 可能还需要清除品牌相关的缓存
            // await _cacheService.RemoveAsync(CacheKeys.GetBrandKey(bearing.BrandId), cancellationToken);

            _logger.LogDebug("轴承更新事件处理完成: {BearingId}", notification.BearingId);
        }
    }
}
