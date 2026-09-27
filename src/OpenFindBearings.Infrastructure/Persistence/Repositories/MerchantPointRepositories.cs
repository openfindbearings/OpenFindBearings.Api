using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 商家金库账户仓储实现（v2.4.0）
    /// </summary>
    public class MerchantPointAccountRepository : IMerchantPointAccountRepository
    {
        private readonly ApplicationDbContext _context;

        public MerchantPointAccountRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public Task<MerchantPointAccount?> GetByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            return _context.Set<MerchantPointAccount>().FirstOrDefaultAsync(a => a.MerchantId == merchantId, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task AddAsync(MerchantPointAccount account, CancellationToken cancellationToken = default)
        {
            await _context.Set<MerchantPointAccount>().AddAsync(account, cancellationToken);
        }

        /// <inheritdoc/>
        public Task UpdateAsync(MerchantPointAccount account, CancellationToken cancellationToken = default)
        {
            _context.Set<MerchantPointAccount>().Update(account);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<int> DeleteByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<MerchantPointAccount>()
                .Where(a => a.MerchantId == merchantId)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }

    /// <summary>
    /// 商家金库流水仓储实现（v2.4.0）：日/月汇总走索引过滤单条 SQL
    /// </summary>
    public class MerchantPointTransactionRepository : IMerchantPointTransactionRepository
    {
        private readonly ApplicationDbContext _context;

        public MerchantPointTransactionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task AddAsync(MerchantPointTransaction tx, CancellationToken cancellationToken = default)
        {
            await _context.Set<MerchantPointTransaction>().AddAsync(tx, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<bool> ExistsBizIdAsync(string bizId, CancellationToken cancellationToken = default)
        {
            return _context.Set<MerchantPointTransaction>()
                .AnyAsync(t => t.BizId == bizId, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<int> SumCreditSinceAsync(Guid merchantId, string grantType, DateTime sinceUtc,
            CancellationToken cancellationToken = default)
        {
            var sum = await _context.Set<MerchantPointTransaction>()
                .Where(t => t.MerchantId == merchantId && t.Direction == MerchantPointTransaction.DirectionCredit
                            && t.GrantType == grantType && t.CreatedAt >= sinceUtc)
                .SumAsync(t => (int?)t.Amount, cancellationToken);
            return sum ?? 0;
        }

        /// <inheritdoc/>
        public async Task<int> SumCreditAnySinceAsync(Guid merchantId, DateTime sinceUtc, CancellationToken cancellationToken = default)
        {
            var sum = await _context.Set<MerchantPointTransaction>()
                .Where(t => t.MerchantId == merchantId && t.Direction == MerchantPointTransaction.DirectionCredit && t.CreatedAt >= sinceUtc)
                .SumAsync(t => (int?)t.Amount, cancellationToken);
            return sum ?? 0;
        }

        /// <inheritdoc/>
        public async Task<List<(Guid MerchantId, int Total)>> GetTopMerchantsCreditAsync(DateTime sinceUtc, int limit,
            CancellationToken cancellationToken = default)
        {
            // 排行榜：金库入账（trickle+结算+任务奖励全部计入"工会实力"）聚合降序
            var rows = await _context.Set<MerchantPointTransaction>()
                .Where(t => t.Direction == MerchantPointTransaction.DirectionCredit && t.CreatedAt >= sinceUtc && t.IsActive)
                .GroupBy(t => t.MerchantId)
                .Select(g => new { MerchantId = g.Key, Total = g.Sum(t => t.Amount) })
                .OrderByDescending(x => x.Total)
                .Take(limit)
                .ToListAsync(cancellationToken);
            return rows.Select(x => (x.MerchantId, x.Total)).ToList();
        }

        /// <inheritdoc/>
        public async Task<(List<MerchantPointTransaction> Items, int Total)> GetByMerchantPagedAsync(
            Guid merchantId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<MerchantPointTransaction>().Where(t => t.MerchantId == merchantId);
            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            return (items, total);
        }

        /// <inheritdoc/>
        public async Task<int> DeleteByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<MerchantPointTransaction>()
                .Where(t => t.MerchantId == merchantId)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
