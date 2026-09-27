using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Application.Services
{
    /// <summary>成就进度视图项（墙/我的/商户徽章排共用）</summary>
    public record AchievementProgressView(
        string Key, string Name, string Description, string Icon, string Category,
        int Scope, int Target, int Progress, bool Unlocked, DateTime? UnlockedAt,
        bool Rare, bool Hidden, int MetaPoints, string? TitleReward, string? ImageKey,
        bool IsLimited = false, int? LimitedOrdinal = null);

    /// <summary>成就墙视图（目录+本人进度+成就点合计+当前称号）</summary>
    public record AchievementWallView(
        List<AchievementProgressView> Items, int TotalMetaPoints, string? CurrentTitle, int UnlockedCount);

    /// <summary>
    /// 成就服务接口（v2.1.0 成就子系统）：事件驱动累加/设值解锁 + 墙/我的/商户查询。
    /// 成就点（meta）只加不花、读时求和；解锁可发小额可花积分甜头（走 PointsService 幂等）
    /// </summary>
    public interface IAchievementService
    {
        /// <summary>计数累加（如纠错采纳 +1），返回本次新点亮的成就键</summary>
        Task<IReadOnlyList<string>> IncrementAsync(AchievementScope scope, Guid ownerId, string metricKey, int delta, CancellationToken cancellationToken = default);

        /// <summary>仪表设值（取较大值防回退，如连签天数/在售数），返回新点亮键</summary>
        Task<IReadOnlyList<string>> SetGaugeAsync(AchievementScope scope, Guid ownerId, string metricKey, int value, CancellationToken cancellationToken = default);

        /// <summary>
        /// 注册序号限量解锁（v2.8.0 G11）：注册链路按"当前注册序号"调用——凡启用且
        /// IsLimited=true、LimitedOrdinal >= ordinal 的成就直接点亮（绝版不返场，非回调补发）。
        /// 与 MetricKey 计数引擎解耦：限量窗口只认注册序号，不参与事件累加
        /// </summary>
        /// <param name="scope">成就范围（个人）</param>
        /// <param name="ownerId">用户 Id</param>
        /// <param name="ordinal">当前注册序号（1 起）</param>
        /// <returns>本次新点亮的成就键</returns>
        Task<IReadOnlyList<string>> UnlockLimitedByOrdinalAsync(AchievementScope scope, Guid ownerId, int ordinal, CancellationToken cancellationToken = default);

        /// <summary>个人成就墙（全目录+本人进度，隐藏成就未解锁不显示）</summary>
        Task<AchievementWallView> GetWallAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>个人已解锁徽章排（资料页/徽章条）</summary>
        Task<AchievementWallView> GetMyAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>商户徽章排（商户详情/卡片，B2B 信任信号）</summary>
        Task<AchievementWallView> GetMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default);
    }
}
