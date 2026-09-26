using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 成就仓储实现（v2.1.0 成就子系统）
    /// </summary>
    public class AchievementRepository : IAchievementRepository
    {
        private readonly ApplicationDbContext _context;

        public AchievementRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AchievementDefinition>> GetEnabledAsync(AchievementScope? scope = null, CancellationToken cancellationToken = default)
        {
            var q = _context.Set<AchievementDefinition>().Where(a => a.Enabled && a.IsActive);
            if (scope.HasValue)
                q = q.Where(a => a.Scope == scope.Value);
            return await q.OrderBy(a => a.Category).ThenBy(a => a.ProgressTarget).ToListAsync(cancellationToken);
        }

        public async Task<List<AchievementDefinition>> GetAllDefinitionsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Set<AchievementDefinition>()
                .OrderBy(a => a.Scope).ThenBy(a => a.Category).ThenBy(a => a.Key)
                .ToListAsync(cancellationToken);
        }

        public Task<AchievementDefinition?> GetDefinitionByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _context.Set<AchievementDefinition>().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<List<AchievementUnlock>> GetUnlocksAsync(AchievementScope scope, Guid ownerId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<AchievementUnlock>()
                .Where(a => a.Scope == scope && a.OwnerId == ownerId)
                .ToListAsync(cancellationToken);
        }

        public Task<AchievementUnlock?> GetUnlockAsync(AchievementScope scope, Guid ownerId, string achievementKey, CancellationToken cancellationToken = default)
        {
            return _context.Set<AchievementUnlock>()
                .FirstOrDefaultAsync(a => a.Scope == scope && a.OwnerId == ownerId && a.AchievementKey == achievementKey, cancellationToken);
        }

        public async Task AddUnlockAsync(AchievementUnlock unlock, CancellationToken cancellationToken = default)
        {
            await _context.Set<AchievementUnlock>().AddAsync(unlock, cancellationToken);
        }

        public void UpdateUnlock(AchievementUnlock unlock)
        {
            _context.Set<AchievementUnlock>().Update(unlock);
        }

        public void UpdateDefinition(AchievementDefinition definition)
        {
            _context.Set<AchievementDefinition>().Update(definition);
        }
    }
}
