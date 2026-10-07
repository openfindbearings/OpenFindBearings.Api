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
            int page, int pageSize, bool pinFirst = false, string? brand = null, string? region = null,
            bool newestFirst = true, CancellationToken cancellationToken = default)
        {
            // 改动说明（v2.12.0 列表删除）：feed/Admin 列表排除软删单（软删仅发布方视角隐藏，数据保留）
            var query = _context.Set<SourcingDemand>().Where(d => !d.IsDeleted).AsQueryable();

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
            // 改动说明（v1.4.0 大厅筛选）：品牌/收货地区均为发布时自由文本，按包含匹配（ToLower 翻译约束同上）
            if (!string.IsNullOrWhiteSpace(brand))
            {
                var b = brand.Trim().ToLower();
                query = query.Where(d => d.Brand != null && d.Brand.ToLower().Contains(b));
            }
            if (!string.IsNullOrWhiteSpace(region))
            {
                var r = region.Trim().ToLower();
                query = query.Where(d => d.Region != null && d.Region.ToLower().Contains(r));
            }

            var total = await query.CountAsync(cancellationToken);
            // 改动说明（v2.10.0 寻货置顶）：公开大厅 pinFirst 时有效置顶排前——
            // 排序键取"未过期的 PinnedUntil，否则最小时间"，过期旧值不插队；
            // v1.4.0 新增 newestFirst：发布时间升降序切换（置顶仍恒排前，付费曝光不受排序影响）
            var ordered = pinFirst
                ? (newestFirst
                    ? query.OrderByDescending(d => d.PinnedUntil != null && d.PinnedUntil > DateTime.UtcNow
                        ? d.PinnedUntil : DateTime.MinValue)
                        .ThenByDescending(d => d.CreatedAt)
                    : query.OrderByDescending(d => d.PinnedUntil != null && d.PinnedUntil > DateTime.UtcNow
                        ? d.PinnedUntil : DateTime.MinValue)
                        .ThenBy(d => d.CreatedAt))
                : (newestFirst
                    ? query.OrderByDescending(d => d.CreatedAt)
                    : query.OrderBy(d => d.CreatedAt));
            var items = await ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            return (items, total);
        }

        /// <inheritdoc/>
        public async Task<List<SourcingDemand>> GetByPublisherAsync(Guid userId, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingDemand>()
                // 改动说明（v2.12.0 商户名义发布）：排除商户名义单（PublisherMerchantId 非空）——
                // 个人"我的寻货"只看个人名义，商户单归商户工作台，双体系不混排
                .Where(d => d.PublisherUserId == userId && d.PublisherMerchantId == null && !d.IsDeleted)
                .OrderByDescending(d => d.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task<List<SourcingDemand>> GetByPublisherMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingDemand>()
                .Where(d => d.PublisherMerchantId == merchantId && !d.IsDeleted)
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
        public Task RemoveAsync(SourcingResponse response, CancellationToken cancellationToken = default)
        {
            _context.Set<SourcingResponse>().Remove(response);
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
        public async Task<int> CountRespondedTodayByUserAsync(Guid userId, CancellationToken cancellationToken = default)
            // 改动说明（v2.7.0 G2 三件套）：按操作人统计今日新增应答（业务日界起点口径与额度评估一致）
            => await _context.Set<SourcingResponse>()
                .CountAsync(r => r.RespondedUserId == userId && r.CreatedAt >= BusinessClock.TodayUtc, cancellationToken);

        /// <inheritdoc/>
        public async Task<List<SourcingResponse>> GetPendingByDemandAsync(Guid demandId, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingResponse>()
                .Where(r => r.DemandId == demandId && r.Status == SourcingResponse.StatusPending)
                .ToListAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task<List<SourcingResponseItem>> GetItemsAsync(Guid responseId, CancellationToken cancellationToken = default)
            => await _context.Set<SourcingResponseItem>()
                .Where(i => i.ResponseId == responseId)
                .OrderBy(i => i.CreatedAt)
                .ToListAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task DeleteItemsAsync(Guid responseId, CancellationToken cancellationToken = default)
        {
            // 改动说明（v1.5.0 多行标书）：重复应答=整体替换行，先清旧行再写新行
            var items = await _context.Set<SourcingResponseItem>()
                .Where(i => i.ResponseId == responseId)
                .ToListAsync(cancellationToken);
            _context.Set<SourcingResponseItem>().RemoveRange(items);
        }

        /// <inheritdoc/>
        public async Task AddItemsAsync(IEnumerable<SourcingResponseItem> items, CancellationToken cancellationToken = default)
            // 改动说明（v1.5.0 多行标书）：显式 Add 行，不走聚合根导航集合，
            // 规避 EF"导航发现新实体+更新根"的 Modified 误判陷阱
            => await _context.Set<SourcingResponseItem>().AddRangeAsync(items, cancellationToken);
    }
}
