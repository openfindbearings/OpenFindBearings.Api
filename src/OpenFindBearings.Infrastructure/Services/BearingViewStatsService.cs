using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Constants;
using OpenFindBearings.Application.Interfaces;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Infrastructure.Services
{
    /// <summary>
    /// 轴承查看统计服务实现
    /// </summary>
    public class BearingViewStatsService : IBearingViewStatsService
    {
        private readonly IBearingRepository _bearingRepository;
        private readonly ICacheService _cacheService;
        private readonly ILogger<BearingViewStatsService> _logger;

        public BearingViewStatsService(
            IBearingRepository bearingRepository,
            ICacheService cacheService,
            ILogger<BearingViewStatsService> logger)
        {
            _bearingRepository = bearingRepository;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task RecordViewAsync(Guid bearingId, Guid? userId, string? sessionId, DateTime viewedAt, CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. 更新数据库浏览次数
                var bearing = await _bearingRepository.GetByIdAsync(bearingId, cancellationToken);
                if (bearing != null)
                {
                    bearing.IncrementViewCount(userId, sessionId);
                    await _bearingRepository.UpdateAsync(bearing, cancellationToken);
                }

                // 2. 更新缓存（用于热门产品）
                await UpdateHotBearingsCacheAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录轴承查看失败: {BearingId}", bearingId);
            }
        }

        public async Task<long> GetViewCountAsync(Guid bearingId, CancellationToken cancellationToken = default)
        {
            var bearing = await _bearingRepository.GetByIdAsync(bearingId, cancellationToken);
            return bearing?.ViewCount ?? 0;
        }

        public async Task<List<Guid>> GetHotBearingsAsync(int count, CancellationToken cancellationToken = default)
        {
            //var cacheKey = $"hot_bearings_{count}";
            var cacheKey = CacheKeys.GetHotBearingsKey(count);
            var cached = await _cacheService.GetAsync<List<Guid>>(cacheKey, cancellationToken);

            if (cached != null)
                return cached;

            var hotBearings = (await _bearingRepository.GetHotBearingsAsync(count * 2, cancellationToken))
                .OrderByDescending(b => b.ViewCount)
                .Take(count)
                .Select(b => b.Id)
                .ToList();

            await _cacheService.SetAsync(cacheKey, hotBearings, TimeSpan.FromHours(1), cancellationToken);

            return hotBearings;
        }

        private async Task UpdateHotBearingsCacheAsync(CancellationToken cancellationToken)
        {
            //await _cacheService.RemoveAsync("hot_bearings_4", cancellationToken);
            //await _cacheService.RemoveAsync("hot_bearings_8", cancellationToken);
            //await _cacheService.RemoveAsync("hot_bearings_12", cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.GetHotBearingsKey(4), cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.GetHotBearingsKey(8), cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.GetHotBearingsKey(12), cancellationToken);
        }
    }
}
