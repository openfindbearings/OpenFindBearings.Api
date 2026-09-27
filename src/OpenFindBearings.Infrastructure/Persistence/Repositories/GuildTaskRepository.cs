using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 工会集体任务仓储实现（v2.6.0）：定义/台账读写，全部显式仓储路径
    /// </summary>
    public class GuildTaskRepository : IGuildTaskRepository
    {
        private readonly ApplicationDbContext _context;

        public GuildTaskRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public Task<List<GuildTaskDefinition>> GetEnabledAsync(CancellationToken cancellationToken = default)
        {
            return _context.Set<GuildTaskDefinition>()
                .Where(t => t.Enabled && t.IsActive)
                .OrderBy(t => t.SortOrder)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<List<GuildTaskDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return _context.Set<GuildTaskDefinition>()
                .OrderBy(t => t.SortOrder)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<GuildTaskDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _context.Set<GuildTaskDefinition>().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task AddDefinitionAsync(GuildTaskDefinition definition, CancellationToken cancellationToken = default)
        {
            await _context.Set<GuildTaskDefinition>().AddAsync(definition, cancellationToken);
        }

        /// <inheritdoc/>
        public Task UpdateDefinitionAsync(GuildTaskDefinition definition, CancellationToken cancellationToken = default)
        {
            _context.Set<GuildTaskDefinition>().Update(definition);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<HashSet<string>> GetCompletedTaskKeysAsync(Guid merchantId, string periodKey,
            CancellationToken cancellationToken = default)
        {
            var keys = await _context.Set<GuildTaskCompletion>()
                .Where(c => c.MerchantId == merchantId && c.PeriodKey == periodKey && c.IsActive)
                .Select(c => c.TaskKey)
                .ToListAsync(cancellationToken);
            return keys.ToHashSet(StringComparer.Ordinal);
        }

        /// <inheritdoc/>
        public async Task AddCompletionAsync(GuildTaskCompletion completion, CancellationToken cancellationToken = default)
        {
            await _context.Set<GuildTaskCompletion>().AddAsync(completion, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<int> CountCompletionsAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<GuildTaskCompletion>()
                .CountAsync(c => c.MerchantId == merchantId && c.IsActive, cancellationToken);
        }
    }
}
