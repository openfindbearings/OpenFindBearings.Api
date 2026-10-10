using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Shared.Interfaces;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Services
{
    /// <summary>
    /// 成就服务实现（v2.1.0 成就子系统）：事件驱动累加/设值→跨阈值点亮。
    /// 成就点（meta）不入库累加，读时按已解锁定义的 MetaPoints 求和（永不通胀、不可花）。
    /// 改动说明（v2.12.0 等级玩法）：成就纯荣誉化——解锁不再发放可花积分甜头，
    /// 货币激励统一收拢到段位升档礼一条线（主流平台成就=荣誉不产币，防"成就刷币推等级"循环农场）。
    /// 改动说明（v2.13.1 提交缺失修复）：本服务此前只跟踪不保存——签到端点/注册中间件在
    /// 积分提交之后才调成就、MediatR handler 在 UnitOfWorkBehavior 提交之后才被 Publish，
    /// 导致成就进度自 v2.1.0 起几乎从未落库（真机现象：累计签到 30+ 仍显示 0/30）。
    /// 修复=服务自持 IUnitOfWork 并在每次变更后 SaveChanges，所有调用方一次修好；
    /// 调用方旁路 try/catch 语义不变（提交失败同样吞掉不反噬主流程）
    /// </summary>
    public class AchievementService : IAchievementService
    {
        private readonly IAchievementRepository _repo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AchievementService> _logger;

        public AchievementService(IAchievementRepository repo, IUnitOfWork unitOfWork, ILogger<AchievementService> logger)
        {
            _repo = repo;
            _unitOfWork = unitOfWork;
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
        /// 注册序号限量解锁（v2.8.0 G11）：注册链路按注册序号调用——凡启用且 IsLimited=true、
        /// LimitedOrdinal >= ordinal 的成就直接点亮。与计数引擎解耦（限量窗口只认序号，
        /// 不参与 MetricKey 累加）；执行纪律=绝版不返场，错过窗口的旧用户不补发
        /// </summary>
        public async Task<IReadOnlyList<string>> UnlockLimitedByOrdinalAsync(AchievementScope scope, Guid ownerId, int ordinal, CancellationToken cancellationToken = default)
        {
            var defs = await _repo.GetEnabledAsync(scope, cancellationToken);
            var eligible = defs.Where(d => d.IsLimited && d.LimitedOrdinal.HasValue && ordinal <= d.LimitedOrdinal.Value).ToList();
            if (eligible.Count == 0)
                return Array.Empty<string>();

            var unlockedKeys = new List<string>();
            foreach (var def in eligible)
            {
                var unlock = await _repo.GetUnlockAsync(scope, ownerId, def.Key, cancellationToken);
                if (unlock?.IsUnlocked == true)
                    continue;
                if (unlock == null)
                {
                    unlock = new AchievementUnlock(scope, ownerId, def.Key);
                    await _repo.AddUnlockAsync(unlock, cancellationToken);
                }
                unlock.AddProgress(1);
                _repo.UpdateUnlock(unlock);
                unlockedKeys.Add(def.Key);
                _logger.LogInformation("限量成就点亮: Scope={Scope}, Owner={OwnerId}, Key={Key}, Ordinal={Ordinal}", scope, ownerId, def.Key, ordinal);
            }

            // 改动说明（v2.13.1）：服务自提交，落库限量徽章点亮（调用方为注册中间件，
            // 该链路无后续提交点，此前变更随 DbContext 释放丢失）
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return unlockedKeys;
        }

        /// <summary>
        /// 对匹配 metricKey 的启用成就逐个累加/设值；跨阈值者点亮（v2.12.0 起纯荣誉——只记点亮与
        /// meta 成就点，不发任何货币）。返回新点亮键供调用方 toast
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
                }
            }

            // 改动说明（v2.13.1）：服务自提交——签到端点/注册与登录中间件在积分提交之后才调成就、
            // MediatR handler 在 UnitOfWorkBehavior 提交之后才被 Publish，这些路径的成就变更此前
            // 无人保存（自 v2.1.0 起进度几乎从未落库）。此处统一提交，调用方无需各自 SaveChanges
            await _unitOfWork.SaveChangesAsync(cancellationToken);
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
                    d.Rare, d.Hidden, d.MetaPoints, d.TitleReward, d.ImageKey,
                    d.IsLimited, d.LimitedOrdinal));
            }

            var unlockedDefs = items.Where(i => i.Unlocked).ToList();
            var totalMeta = unlockedDefs.Sum(i => i.MetaPoints);
            var title = unlockedDefs.Where(i => !string.IsNullOrEmpty(i.TitleReward))
                .OrderByDescending(i => i.MetaPoints).Select(i => i.TitleReward).FirstOrDefault();

            return new AchievementWallView(items, totalMeta, title, unlockedDefs.Count);
        }
    }
}
