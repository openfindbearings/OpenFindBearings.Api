using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Services;
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

        /// <summary>
        /// 删除用户积分账户（注销清零，ExecuteDelete 单 SQL）
        /// </summary>
        public async Task<int> DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<PointAccount>()
                .Where(p => p.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);
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
            // 改动说明（v1.36.1 日界修复）：日上限的"当日"从 UTC 日界改为北京日界
            // （BusinessClock.TodayUtc 折回 UTC 后仍是 timestamptz 可比较的 UTC 时刻）
            var today = BusinessClock.TodayUtc;
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

        /// <summary>
        /// 用户各动作流水次数统计（since 非空只看该时刻后）。
        /// 改动说明（v1.34.0）：任务中心 daily 任务显示"今日已完成 n 次"，
        /// 一次 group by 出全部计数，避免逐类型 Count 查询
        /// </summary>
        public async Task<Dictionary<string, int>> GetGrantCountsAsync(Guid userId, DateTime? since, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<PointTransaction>()
                .Where(t => t.UserId == userId && t.Direction == PointTransaction.DirectionCredit);
            if (since.HasValue)
                query = query.Where(t => t.CreatedAt >= since.Value);
            var rows = await query
                .GroupBy(t => t.GrantType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            return rows.ToDictionary(r => r.Type, r => r.Count, StringComparer.OrdinalIgnoreCase);
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
/// <summary>
    /// 一次性奖励认领台账仓储实现（v1.34.0）：原生 INSERT ON CONFLICT，原子幂等
    /// </summary>
    public class PointRewardClaimRepository : IPointRewardClaimRepository
    {
        private readonly ApplicationDbContext _context;

        public PointRewardClaimRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// INSERT ... ON CONFLICT ("BizKey") DO NOTHING，受影响行数=1 即首次认领
        /// </summary>
        public async Task<bool> TryClaimAsync(string bizKey, string grantType, Guid userId, CancellationToken cancellationToken = default)
        {
            var affected = await _context.Database.ExecuteSqlRawAsync(
                @"INSERT INTO ""PointRewardClaims"" (""Id"", ""BizKey"", ""GrantType"", ""FirstClaimerUserId"", ""CreatedAt"", ""IsActive"")
                  VALUES (@p0, @p1, @p2, @p3, now(), true)
                  ON CONFLICT (""BizKey"") DO NOTHING",
                new object[] { Guid.NewGuid(), bizKey, grantType, userId },
                cancellationToken);
            return affected == 1;
        }
    }
}