using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Constants;
using OpenFindBearings.Application.Interfaces;
using OpenFindBearings.Domain.Events;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Application.Features.Bearings.EventHandlers
{
    /// <summary>
    /// 轴承查看事件处理器
    /// 用于统计热门产品、用户行为分析、记录浏览历史
    /// </summary>
    public class BearingViewedEventHandler : INotificationHandler<BearingViewedEvent>
    {
        private readonly ILogger<BearingViewedEventHandler> _logger;
        private readonly IBearingViewStatsService _statsService;
        private readonly IUserBearingHistoryRepository _historyRepository;
        private readonly ICacheService _cacheService;

        public BearingViewedEventHandler(
            ILogger<BearingViewedEventHandler> logger,
            IBearingViewStatsService statsService,
            IUserBearingHistoryRepository historyRepository,
            ICacheService cacheService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _statsService = statsService ?? throw new ArgumentNullException(nameof(statsService));
            _historyRepository = historyRepository ?? throw new ArgumentNullException(nameof(historyRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        public async Task Handle(BearingViewedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogDebug(
                "轴承被查看: 轴承ID={BearingId}, 型号={PartNumber}, 当前浏览次数={ViewCount}, 用户类型={UserType}",
                notification.BearingId,
                notification.PartNumber,
                notification.CurrentViewCount,
                notification.IsAuthenticated ? "登录用户" : "游客");

            try
            {
                // 1. 记录浏览次数统计（用于热门排名）
                await _statsService.RecordViewAsync(
                    bearingId: notification.BearingId,
                    userId: notification.UserId,
                    sessionId: notification.SessionId,
                    viewedAt: notification.ViewedAt,
                    cancellationToken: cancellationToken);

                // 2. 记录用户浏览历史（只有登录用户才记录）
                if (notification.UserId.HasValue)
                {
                    await _historyRepository.AddOrUpdateAsync(
                        userId: notification.UserId.Value,
                        bearingId: notification.BearingId,
                        cancellationToken: cancellationToken);
                }
                // 游客浏览历史暂不记录，或可考虑用 SessionId 记录

                // 3. 更新热门产品缓存
                await UpdateHotProductsCacheAsync(notification.BearingId, notification.CurrentViewCount, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "处理轴承查看事件失败: BearingId={BearingId}", notification.BearingId);
            }
        }

        /// <summary>
        /// 更新热门产品缓存
        /// </summary>
        private async Task UpdateHotProductsCacheAsync(Guid bearingId, int currentViewCount, CancellationToken cancellationToken)
        {
            // 每100次查看，清除热门缓存，让下次查询重新计算
            if (currentViewCount % 100 == 0)
            {
                // 清除热门轴承缓存
                var cacheKeys = new[] { 4, 8, 12, 20 };
                foreach (var count in cacheKeys)
                {
                    var cacheKey = CacheKeys.GetHotBearingsKey(count);
                    await _cacheService.RemoveAsync(cacheKey, cancellationToken);
                }

                _logger.LogDebug("热门轴承缓存已清除，当前浏览次数: {ViewCount}", currentViewCount);
            }
        }
    }
}
