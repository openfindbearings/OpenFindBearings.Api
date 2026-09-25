using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 积分账户仓储实现（v1.32.0）
    /// </summary>
    public class PointAccountRepository : IPointAccountRepository
    {
        private readonly ApplicationDbContext _context;

        public PointAccountRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<PointAccount?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return _context.Set<PointAccount>()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        }

        public async Task AddAsync(PointAccount account, CancellationToken cancellationToken = default)
        {
            await _context.Set<PointAccount>().AddAsync(account, cancellationToken);
        }

        public Task UpdateAsync(PointAccount account, CancellationToken cancellationToken = default)
        {
            _context.Set<PointAccount>().Update(account);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 积分流水仓储实现（v1.32.0）
    /// </summary>
    public class PointTransactionRepository : IPointTransactionRepository
    {
        private readonly ApplicationDbContext _context;

        public PointTransactionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(PointTransaction transaction, CancellationToken cancellationToken = default)
        {
            await _context.Set<PointTransaction>().AddAsync(transaction, cancellationToken);
        }

        public Task<bool> ExistsBizIdAsync(string bizId, CancellationToken cancellationToken = default)
        {
            return _context.Set<PointTransaction>().AnyAsync(t => t.BizId == bizId, cancellationToken);
        }

        public async Task<int> SumTodayByTypeAsync(Guid userId, string grantType, CancellationToken cancellationToken = default)
        {
            // 当日边界用 UTC 日期（全项目 UTC 规范）
            var today = DateTime.UtcNow.Date;
            var sum = await _context.Set<PointTransaction>()
                .Where(t => t.UserId == userId && t.GrantType == grantType
                    && t.Direction == PointTransaction.DirectionCredit && t.CreatedAt >= today)
                .SumAsync(t => (int?)t.Amount, cancellationToken);
            return sum ?? 0;
        }

        public async Task<(List<PointTransaction> Items, int Total)> GetByUserAsync(Guid userId, int page, int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Set<PointTransaction>()
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt);
            var total = await query.CountAsync(cancellationToken);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return (items, total);
        }

        /// <summary>
        /// 用户有流水的动作类型去重集合（since 非空只看该时刻后，任务中心完成态批量判定）
        /// </summary>
        public async Task<HashSet<string>> GetGrantTypesAsync(Guid userId, DateTime? since, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<PointTransaction>()
                .Where(t => t.UserId == userId && t.Direction == PointTransaction.DirectionCredit);
            if (since.HasValue)
                query = query.Where(t => t.CreatedAt >= since.Value);
            var types = await query.Select(t => t.GrantType).Distinct().ToListAsync(cancellationToken);
            return types.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public async Task<int> DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<PointTransaction>()
                .Where(t => t.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }

    /// <summary>
    /// 积分规则仓储实现（v1.32.0）
    /// </summary>
    public class PointGrantRuleRepository : IPointGrantRuleRepository
    {
        private readonly ApplicationDbContext _context;

        public PointGrantRuleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<PointGrantRule?> GetEnabledByTypeAsync(string grantType, CancellationToken cancellationToken = default)
        {
            return _context.Set<PointGrantRule>()
                .FirstOrDefaultAsync(r => r.GrantType == grantType && r.IsEnabled, cancellationToken);
        }

        public async Task<List<PointGrantRule>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Set<PointGrantRule>()
                .OrderBy(r => r.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(PointGrantRule rule, CancellationToken cancellationToken = default)
        {
            await _context.Set<PointGrantRule>().AddAsync(rule, cancellationToken);
        }

        public Task UpdateAsync(PointGrantRule rule, CancellationToken cancellationToken = default)
        {
            _context.Set<PointGrantRule>().Update(rule);
            return Task.CompletedTask;
        }
    }
}
