using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 商家集体任务仓储实现（v2.6.0）：定义/台账读写，全部显式仓储路径
    /// </summary>
    public class MerchantTaskRepository : IMerchantTaskRepository
    {
        private readonly ApplicationDbContext _context;

        public MerchantTaskRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public Task<List<MerchantTaskDefinition>> GetEnabledAsync(CancellationToken cancellationToken = default)
        {
            return _context.Set<MerchantTaskDefinition>()
                .Where(t => t.Enabled && t.IsActive)
                .OrderBy(t => t.SortOrder)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<List<MerchantTaskDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return _context.Set<MerchantTaskDefinition>()
                .OrderBy(t => t.SortOrder)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<MerchantTaskDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _context.Set<MerchantTaskDefinition>().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task AddDefinitionAsync(MerchantTaskDefinition definition, CancellationToken cancellationToken = default)
        {
            await _context.Set<MerchantTaskDefinition>().AddAsync(definition, cancellationToken);
        }

        /// <inheritdoc/>
        public Task UpdateDefinitionAsync(MerchantTaskDefinition definition, CancellationToken cancellationToken = default)
        {
            _context.Set<MerchantTaskDefinition>().Update(definition);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<HashSet<string>> GetCompletedTaskKeysAsync(Guid merchantId, string periodKey,
            CancellationToken cancellationToken = default)
        {
            var keys = await _context.Set<MerchantTaskCompletion>()
                .Where(c => c.MerchantId == merchantId && c.PeriodKey == periodKey && c.IsActive)
                .Select(c => c.TaskKey)
                .ToListAsync(cancellationToken);
            return keys.ToHashSet(StringComparer.Ordinal);
        }

        /// <inheritdoc/>
        public async Task AddCompletionAsync(MerchantTaskCompletion completion, CancellationToken cancellationToken = default)
        {
            await _context.Set<MerchantTaskCompletion>().AddAsync(completion, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<int> CountCompletionsAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<MerchantTaskCompletion>()
                .CountAsync(c => c.MerchantId == merchantId && c.IsActive, cancellationToken);
        }
    }
}
