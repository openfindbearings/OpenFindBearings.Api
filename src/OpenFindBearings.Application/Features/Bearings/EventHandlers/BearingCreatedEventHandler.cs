using MediatR;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Interfaces;
using OpenFindBearings.Application.Shared.Constants;
using OpenFindBearings.Domain.Events;

namespace OpenFindBearings.Application.Features.Bearings.EventHandlers
{
    /// <summary>
    /// 轴承创建事件处理器
    /// 当新轴承添加到系统时触发
    /// </summary>
    public class BearingCreatedEventHandler : INotificationHandler<BearingCreatedEvent>
    {
        private readonly ILogger<BearingCreatedEventHandler> _logger;
        private readonly ICacheService _cacheService;
        private readonly INotificationService _notificationService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="cacheService">缓存服务</param>
        /// <param name="notificationService">通知服务</param>
        public BearingCreatedEventHandler(
            ILogger<BearingCreatedEventHandler> logger,
            ICacheService cacheService,
            INotificationService notificationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        /// <summary>
        /// 处理轴承创建事件
        /// </summary>
        /// <param name="notification">轴承创建事件</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task Handle(BearingCreatedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "处理轴承创建事件: 轴承ID={BearingId}, 型号={PartNumber}, 品牌ID={BrandId}, 时间={OccurredOn}",
                notification.BearingId,
                notification.PartNumber,
                notification.BrandId,
                notification.OccurredOn);

            // 1. 更新缓存
            await UpdateBearingCacheAsync(notification, cancellationToken);

            // 2. 发送通知（可选）
            await SendNewBearingNotificationAsync(notification, cancellationToken);

            // 3. 更新统计信息
            await UpdateStatisticsAsync(notification, cancellationToken);

            _logger.LogDebug("轴承创建事件处理完成: {BearingId}", notification.BearingId);
        }

        /// <summary>
        /// 更新轴承缓存
        /// </summary>
        private async Task UpdateBearingCacheAsync(BearingCreatedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                // 清除相关缓存
                //await _cacheService.RemoveAsync("recent_bearings", cancellationToken);
                //await _cacheService.RemoveAsync($"brand_{notification.BrandId}_bearings", cancellationToken);
                await _cacheService.RemoveAsync(CacheKeys.RecentBearings, cancellationToken);

                // 清除品牌相关的轴承列表缓存
                await _cacheService.RemoveAsync(CacheKeys.GetBrandBearingsKey(notification.BrandId), cancellationToken);

                // 清除品牌统计缓存
                await _cacheService.RemoveAsync(CacheKeys.GetBrandStatisticsKey(notification.BrandId), cancellationToken);

                // 如果Redis支持模式匹配，还可以清除所有品牌相关的缓存
                // await _cacheService.RemoveByPatternAsync(CacheKeys.Patterns.ForBrand(notification.BrandId), cancellationToken);

                // 清除全局统计缓存（因为轴承总数变化了）
                await _cacheService.RemoveAsync(CacheKeys.GetGlobalStatisticsKey(), cancellationToken);

                // 清除搜索结果缓存（因为新增轴承会影响搜索结果）
                // 注意：这里可能需要更精细的控制，比如只清除特定品牌的搜索结果
                // await _cacheService.RemoveByPatternAsync(CacheKeys.Patterns.AllSearchResults, cancellationToken);

                _logger.LogDebug("轴承缓存已清除: 品牌ID={BrandId}", notification.BrandId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "更新轴承缓存失败: {BearingId}", notification.BearingId);
                // 缓存失败不影响主流程
            }
        }

        /// <summary>
        /// 发送新轴承通知
        /// </summary>
        private async Task SendNewBearingNotificationAsync(BearingCreatedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                // 可以根据业务需要决定是否发送通知
                // 比如：新品牌首型导入时通知管理员

                // await _notificationService.SendToAdminsAsync(
                //     "新轴承添加",
                //     $"新增轴承型号: {notification.PartNumber}",
                //     cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "发送新轴承通知失败: {BearingId}", notification.BearingId);
            }
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private async Task UpdateStatisticsAsync(BearingCreatedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                // 更新品牌统计
                await UpdateBrandStatisticsAsync(notification.BrandId, cancellationToken);

                // 更新总体统计
                await UpdateGlobalStatisticsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "更新统计信息失败: {BearingId}", notification.BearingId);
            }
        }

        /// <summary>
        /// 更新品牌统计
        /// </summary>
        private async Task UpdateBrandStatisticsAsync(Guid brandId, CancellationToken cancellationToken)
        {
            // 可以在缓存中维护品牌的产品数量统计
            //var key = $"brand_{brandId}_stats";
            var key = CacheKeys.GetBrandStatisticsKey(brandId);
            var stats = await _cacheService.GetAsync<BrandStatistics>(key, cancellationToken)
                        ?? new BrandStatistics();

            stats.TotalBearings++;
            stats.LastBearingAddedAt = DateTime.UtcNow;

            await _cacheService.SetAsync(key, stats, TimeSpan.FromHours(1), cancellationToken);
        }

        /// <summary>
        /// 更新全局统计
        /// </summary>
        private async Task UpdateGlobalStatisticsAsync(CancellationToken cancellationToken)
        {
            //var key = "global_statistics";
            var key = CacheKeys.GetGlobalStatisticsKey();
            var stats = await _cacheService.GetAsync<GlobalStatistics>(key, cancellationToken)
                        ?? new GlobalStatistics();

            stats.TotalBearings++;
            stats.LastUpdatedAt = DateTime.UtcNow;

            await _cacheService.SetAsync(key, stats, TimeSpan.FromHours(1), cancellationToken);
        }
    }

    /// <summary>
    /// 品牌统计信息
    /// </summary>
    public class BrandStatistics
    {
        public int TotalBearings { get; set; }
        public DateTime LastBearingAddedAt { get; set; }
    }

    /// <summary>
    /// 全局统计信息
    /// </summary>
    public class GlobalStatistics
    {
        public int TotalBearings { get; set; }
        public int TotalMerchants { get; set; }
        public int TotalUsers { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }
}
