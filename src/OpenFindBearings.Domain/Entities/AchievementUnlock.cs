using OpenFindBearings.Domain.Abstractions;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 成就解锁/进度记录（v2.1.0 成就子系统）：一行=某主体（用户或商户）对某成就的进度与解锁态。
    /// (Scope,OwnerId,AchievementKey) 唯一索引=幂等防线（同主体同成就仅一行，进度累加不重复建行）。
    /// 个人轨 OwnerId=UserId、商户轨 OwnerId=MerchantId，一表双轨
    /// </summary>
    public class AchievementUnlock : BaseEntity
    {
        /// <summary>归属范围（个人/商户），与定义的 Scope 一致</summary>
        public AchievementScope Scope { get; private set; }

        /// <summary>主体 Id（个人=UserId，商户=MerchantId）</summary>
        public Guid OwnerId { get; private set; }

        /// <summary>成就键（关联 AchievementDefinition.Key）</summary>
        public string AchievementKey { get; private set; } = string.Empty;

        /// <summary>当前进度（计数累加或仪表值）</summary>
        public int Progress { get; private set; }

        /// <summary>解锁时间（null=未解锁）</summary>
        public DateTime? UnlockedAt { get; private set; }

        /// <summary>EF 专用无参构造</summary>
        protected AchievementUnlock() { }

        /// <summary>
        /// 创建进度记录（初始 0 进度、未解锁）
        /// </summary>
        public AchievementUnlock(AchievementScope scope, Guid ownerId, string achievementKey)
        {
            Scope = scope;
            OwnerId = ownerId;
            AchievementKey = achievementKey;
            Progress = 0;
        }

        /// <summary>是否已解锁</summary>
        public bool IsUnlocked => UnlockedAt.HasValue;

        /// <summary>
        /// 计数累加；若跨过阈值则点亮并返回 true（调用方据此发解锁奖励/toast）
        /// </summary>
        public bool AddProgress(int delta)
        {
            Progress += delta;
            return TryUnlock();
        }

        /// <summary>
        /// 仪表设值（取较大值，防回退，如连签天数/在售数）；跨阈值则点亮并返回 true
        /// </summary>
        public bool SetGauge(int value)
        {
            if (value > Progress)
                Progress = value;
            return TryUnlock();
        }

        /// <summary>达到目标且未解锁时点亮</summary>
        private bool TryUnlock()
        {
            if (IsUnlocked)
                return false;
            UnlockedAt = DateTime.UtcNow;
            return true;
        }
    }
}
