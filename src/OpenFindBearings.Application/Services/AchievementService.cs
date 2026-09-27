using Microsoft.Extensions.Logging;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Services
{
    /// <summary>
    /// 成就服务实现（v2.1.0 成就子系统）：事件驱动累加/设值→跨阈值点亮→发解锁甜头。
    /// 成就点（meta）不入库累加，读时按已解锁定义的 MetaPoints 求和（永不通胀、不可花）；
    /// 可花甜头走 PointsService（bizId=ach:{key}:{owner} 幂等，重复解锁不重发）
    /// </summary>
    public class AchievementService : IAchievementService
    {
        private readonly IAchievementRepository _repo;
        private readonly IPointsService _pointsService;
        private readonly ILogger<AchievementService> _logger;

        public AchievementService(IAchievementRepository repo, IPointsService pointsService, ILogger<AchievementService> logger)
        {
            _repo = repo;
            _pointsService = pointsService;
            _logger = logger;
        }

        /// <summary>计数累加并判定解锁</summary>
        public Task<IReadOnlyList<string>> IncrementAsync(AchievementScope scope, Guid ownerId, string metricKey, int delta, CancellationToken cancellationToken = default)
        {
            return ApplyAsync(scope, ownerId, metricKey, u => u.AddProgress(delta), cancellationToken);
        }

        /// <summary>仪表设值并判定解锁</summary>
        public Task<IReadOnlyList<string>> SetGaugeAsync(AchievementScope scope, Guid ownerId, string metricKey, int value, CancellationToken cancellationToken = default)
        {
            return ApplyAsync(scope, ownerId, metricKey, u => u.SetGauge(value), cancellationToken);
        }

        /// <summary>
        /// 对匹配 metricKey 的启用成就逐个累加/设值；跨阈值者点亮并发解锁甜头（仅个人轨发可花积分，
        /// 商户轨 M1-a 仅记 meta，金库结算留 M1-c）。返回新点亮键供调用方 toast
        /// </summary>
        private async Task<IReadOnlyList<string>> ApplyAsync(AchievementScope scope, Guid ownerId, string metricKey,
            Func<AchievementUnlock, bool> mutate, CancellationToken cancellationToken)
        {
            var defs = await _repo.GetEnabledAsync(scope, cancellationToken);
            var matched = defs.Where(d => d.MetricKey == metricKey).ToList();
            if (matched.Count == 0)
                return Array.Empty<string>();

            var unlockedKeys = new List<string>();
            foreach (var def in matched)
            {
                var unlock = await _repo.GetUnlockAsync(scope, ownerId, def.Key, cancellationToken);
                if (unlock == null)
                {
                    unlock = new AchievementUnlock(scope, ownerId, def.Key);
                    await _repo.AddUnlockAsync(unlock, cancellationToken);
                }

                var unlockedNow = mutate(unlock);
                _repo.UpdateUnlock(unlock);

                if (unlockedNow)
                {
                    unlockedKeys.Add(def.Key);
                    _logger.LogInformation("成就点亮: Scope={Scope}, Owner={OwnerId}, Key={Key}", scope, ownerId, def.Key);

                    // 个人轨解锁甜头：小额可花积分，幂等键防重发；商户轨不发（金库 M1-c）
                    if (scope == AchievementScope.Personal && def.RewardPoints > 0)
                    {
                        await _pointsService.GrantAsync(ownerId, PointTransaction.TypeAchievementUnlock,
                            $"ach:{def.Key}:{ownerId:N}", $"成就解锁：{def.Name}", def.RewardPoints, cancellationToken);
                    }
                }
            }

            return unlockedKeys;
        }

        /// <summary>个人成就墙：全目录+本人进度；隐藏成就未解锁不显示</summary>
        public async Task<AchievementWallView> GetWallAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await BuildViewAsync(AchievementScope.Personal, userId, wall: true, cancellationToken);
        }

        /// <summary>个人已解锁徽章排</summary>
        public async Task<AchievementWallView> GetMyAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await BuildViewAsync(AchievementScope.Personal, userId, wall: false, cancellationToken);
        }

        /// <summary>商户徽章排</summary>
        public async Task<AchievementWallView> GetMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await BuildViewAsync(AchievementScope.Merchant, merchantId, wall: false, cancellationToken);
        }

        /// <summary>
        /// 组装视图：wall=true 返回全目录（隐藏未解锁剔除）带进度；wall=false 仅已解锁（徽章排）。
        /// meta 成就点=已解锁定义 MetaPoints 求和；当前称号=已解锁中带称号者按 meta 降序取首
        /// </summary>
        private async Task<AchievementWallView> BuildViewAsync(AchievementScope scope, Guid ownerId, bool wall, CancellationToken cancellationToken)
        {
            var defs = await _repo.GetEnabledAsync(scope, cancellationToken);
            var unlocks = await _repo.GetUnlocksAsync(scope, ownerId, cancellationToken);
            var unlockMap = unlocks.ToDictionary(u => u.AchievementKey, u => u);

            var items = new List<AchievementProgressView>();
            foreach (var d in defs)
            {
                unlockMap.TryGetValue(d.Key, out var u);
                var unlocked = u?.IsUnlocked == true;
                if (!wall && !unlocked)
                    continue;
                if (wall && d.Hidden && !unlocked)
                    continue;

                items.Add(new AchievementProgressView(
                    d.Key, d.Name, d.Description, d.Icon, d.Category,
                    (int)d.Scope, d.ProgressTarget, u?.Progress ?? 0, unlocked, u?.UnlockedAt,
                    d.Rare, d.Hidden, d.MetaPoints, d.TitleReward, d.ImageKey));
            }

            var unlockedDefs = items.Where(i => i.Unlocked).ToList();
            var totalMeta = unlockedDefs.Sum(i => i.MetaPoints);
            var title = unlockedDefs.Where(i => !string.IsNullOrEmpty(i.TitleReward))
                .OrderByDescending(i => i.MetaPoints).Select(i => i.TitleReward).FirstOrDefault();

            return new AchievementWallView(items, totalMeta, title, unlockedDefs.Count);
        }
    }
}
