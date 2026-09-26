using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    /// <summary>
    /// 成就仓储接口（v2.1.0 成就子系统）：定义目录 + 一表双轨解锁进度
    /// </summary>
    public interface IAchievementRepository
    {
        /// <summary>启用中的成就定义（可按范围过滤，null=全部）</summary>
        Task<List<AchievementDefinition>> GetEnabledAsync(AchievementScope? scope = null, CancellationToken cancellationToken = default);

        /// <summary>全部定义（Admin 管理用，含停用）</summary>
        Task<List<AchievementDefinition>> GetAllDefinitionsAsync(CancellationToken cancellationToken = default);

        /// <summary>按 Id 取定义（Admin 编辑）</summary>
        Task<AchievementDefinition?> GetDefinitionByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>某主体的全部解锁/进度行</summary>
        Task<List<AchievementUnlock>> GetUnlocksAsync(AchievementScope scope, Guid ownerId, CancellationToken cancellationToken = default);

        /// <summary>某主体某成就的进度行（可空）</summary>
        Task<AchievementUnlock?> GetUnlockAsync(AchievementScope scope, Guid ownerId, string achievementKey, CancellationToken cancellationToken = default);

        /// <summary>新建进度行</summary>
        Task AddUnlockAsync(AchievementUnlock unlock, CancellationToken cancellationToken = default);

        /// <summary>标记进度行修改</summary>
        void UpdateUnlock(AchievementUnlock unlock);

        /// <summary>标记定义修改（Admin）</summary>
        void UpdateDefinition(AchievementDefinition definition);
    }
}
