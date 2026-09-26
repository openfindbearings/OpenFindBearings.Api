using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Services;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 寻货需求仓储实现（v1.35.0）
    /// </summary>
    public class SourcingDemandRepository : ISourcingDemandRepository
    {
        private readonly ApplicationDbContext _context;

        public SourcingDemandRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public Task<SourcingDemand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => _context.Set<SourcingDemand>().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        /// <inheritdoc/>
        public async Task AddAsync(SourcingDemand demand, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingDemand>().AddAsync(demand, cancellationToken);

        /// <inheritdoc/>
        public Task UpdateAsync(SourcingDemand demand, CancellationToken cancellationToken = default)
        {
            _context.Set<SourcingDemand>().Update(demand);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<(List<SourcingDemand> Items, int Total)> GetListAsync(int? status, string? keyword, bool onlyOpen,
            int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<SourcingDemand>().AsQueryable();

            if (status.HasValue)
                query = query.Where(d => d.Status == status.Value);
            if (onlyOpen)
                query = query.Where(d => d.Status == SourcingDemand.StatusPublished && d.ExpiryAt > DateTime.UtcNow);
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                // Npgsql 可翻译要求 ToLower（ToLowerInvariant 运行时不可译，既有约束）
                var kw = keyword.Trim().ToLower();
                query = query.Where(d => d.PartNumber.ToLower().Contains(kw));
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            return (items, total);
        }

        /// <inheritdoc/>
        public async Task<List<SourcingDemand>> GetByPublisherAsync(Guid userId, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingDemand>()
                .Where(d => d.PublisherUserId == userId)
                .OrderByDescending(d => d.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task<int> CountPublishedTodayAsync(Guid userId, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingDemand>()
                .CountAsync(d => d.PublisherUserId == userId && d.CreatedAt >= BusinessClock.TodayUtc, cancellationToken);

        /// <inheritdoc/>
        public async Task<int> ExpireOverdueAsync(CancellationToken cancellationToken = default)
        {
            // 惰性过期统一收口：进行中且已过 ExpiryAt → Expired（feed 顶部与详情读时各调一次，成本一条 UPDATE）
            return await _context.Set<SourcingDemand>()
                .Where(d => d.Status == SourcingDemand.StatusPublished && d.ExpiryAt <= DateTime.UtcNow)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.Status, SourcingDemand.StatusExpired)
                    .SetProperty(d => d.ClosedAt, DateTime.UtcNow), cancellationToken);
        }
    }

    /// <summary>
    /// 寻货应答仓储实现（v1.35.0）
    /// </summary>
    public class SourcingResponseRepository : ISourcingResponseRepository
    {
        private readonly ApplicationDbContext _context;

        public SourcingResponseRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public Task<SourcingResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => _context.Set<SourcingResponse>().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        /// <inheritdoc/>
        public async Task AddAsync(SourcingResponse response, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingResponse>().AddAsync(response, cancellationToken);

        /// <inheritdoc/>
        public Task UpdateAsync(SourcingResponse response, CancellationToken cancellationToken = default)
        {
            _context.Set<SourcingResponse>().Update(response);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<List<SourcingResponse>> GetByDemandAsync(Guid demandId, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingResponse>()
                .Where(r => r.DemandId == demandId)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync(cancellationToken);

        /// <inheritdoc/>
        public Task<SourcingResponse?> GetByDemandAndMerchantAsync(Guid demandId, Guid merchantId, CancellationToken cancellationToken = default)
            => _context.Set<SourcingResponse>().FirstOrDefaultAsync(
                r => r.DemandId == demandId && r.MerchantId == merchantId, cancellationToken);

        /// <inheritdoc/>
        public async Task<List<SourcingResponse>> GetByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingResponse>()
                .Where(r => r.MerchantId == merchantId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task<int> CountRespondedTodayAsync(Guid merchantId, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingResponse>()
                .CountAsync(r => r.MerchantId == merchantId && r.CreatedAt >= BusinessClock.TodayUtc, cancellationToken);

        /// <inheritdoc/>
        public async Task<List<SourcingResponse>> GetPendingByDemandAsync(Guid demandId, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingResponse>()
                .Where(r => r.DemandId == demandId && r.Status == SourcingResponse.StatusPending)
                .ToListAsync(cancellationToken);
    }
}
