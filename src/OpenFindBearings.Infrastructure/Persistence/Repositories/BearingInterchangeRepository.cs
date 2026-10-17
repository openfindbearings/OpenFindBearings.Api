using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;
using OpenFindBearings.Infrastructure.Persistence.Extensions;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    public class BearingInterchangeRepository : IBearingInterchangeRepository
    {
        private readonly ApplicationDbContext _context;

        public BearingInterchangeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BearingInterchange?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.BearingInterchanges
                .Include(bi => bi.SourceBearing)
                .Include(bi => bi.TargetBearing)
                .FirstOrDefaultAsync(bi => bi.Id == id, cancellationToken);
        }

        public async Task<List<BearingInterchange>> GetBySourceBearingAsync(Guid bearingId, CancellationToken cancellationToken = default)
        {
            return await _context.BearingInterchanges
                .Include(bi => bi.TargetBearing)
                    .ThenInclude(b => b.Brand)
                .Include(bi => bi.TargetBearing)
                    .ThenInclude(b => b.BearingTypeNavigation)
                .Where(bi => bi.SourceBearingId == bearingId && bi.IsActive)
                .OrderByDescending(bi => bi.Confidence)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<BearingInterchange>> GetByTargetBearingAsync(Guid bearingId, CancellationToken cancellationToken = default)
        {
            return await _context.BearingInterchanges
                .Include(bi => bi.SourceBearing)
                    .ThenInclude(b => b.Brand)
                .Include(bi => bi.SourceBearing)
                    .ThenInclude(b => b.BearingTypeNavigation)
                .Where(bi => bi.TargetBearingId == bearingId && bi.IsActive)
                .OrderByDescending(bi => bi.Confidence)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken = default)
        {
            return await _context.BearingInterchanges
                .AnyAsync(bi => bi.SourceBearingId == sourceId && bi.TargetBearingId == targetId, cancellationToken);
        }

        public async Task AddAsync(BearingInterchange interchange, CancellationToken cancellationToken = default)
        {
            await _context.BearingInterchanges.AddAsync(interchange, cancellationToken);
        }

        public async Task AddRangeAsync(List<BearingInterchange> interchanges, CancellationToken cancellationToken = default)
        {
            await _context.BearingInterchanges.AddRangeAsync(interchanges, cancellationToken);
        }

        public async Task UpdateAsync(BearingInterchange interchange, CancellationToken cancellationToken = default)
        {
            _context.MarkModified(interchange);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            // 改动说明（v2.16.0）：替代关系删除改为物理删除——唯一约束 (Source,Target) 彻底释放，
            // 同对型号可再次录入；替代品为无软删历史的目录数据，不保留软删行
            var interchange = await _context.BearingInterchanges
                .FirstOrDefaultAsync(bi => bi.Id == id, cancellationToken);
            if (interchange != null)
            {
                _context.BearingInterchanges.Remove(interchange);
            }
        }

        public async Task<List<BearingInterchange>> GetApprovedForBearingAsync(Guid bearingId, CancellationToken cancellationToken = default)
        {
            // 正向（该轴承为源）+ 反向（该轴承为目标且双边）合并，双向标记因此真正生效
            return await _context.BearingInterchanges
                .Include(bi => bi.SourceBearing).ThenInclude(b => b!.Brand)
                .Include(bi => bi.SourceBearing).ThenInclude(b => b!.BearingTypeNavigation)
                .Include(bi => bi.TargetBearing).ThenInclude(b => b!.Brand)
                .Include(bi => bi.TargetBearing).ThenInclude(b => b!.BearingTypeNavigation)
                .Where(bi => bi.IsActive
                    && bi.ReviewStatus == InterchangeReviewStatus.Approved
                    && (bi.SourceBearingId == bearingId
                        || (bi.TargetBearingId == bearingId && bi.IsBidirectional)))
                .OrderByDescending(bi => bi.Confidence)
                .ToListAsync(cancellationToken);
        }

        public async Task<(List<BearingInterchange> Items, int Total)> GetPagedAsync(
            string? keyword, InterchangeReviewStatus? status,
            DateTime? createdFromUtc, DateTime? createdToUtc, bool? internetSource,
            int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.BearingInterchanges.AsQueryable();

            // 改动说明（v1.50.0 下钻筛选）：创建时间区间（下限含、上限不含）与来源（互联网/非互联网）
            if (createdFromUtc.HasValue)
                query = query.Where(bi => bi.CreatedAt >= createdFromUtc.Value);
            if (createdToUtc.HasValue)
                query = query.Where(bi => bi.CreatedAt < createdToUtc.Value);
            if (internetSource.HasValue)
            {
                query = internetSource.Value
                    ? query.Where(bi => bi.DataSource != null && bi.DataSource.SourceType == DataSourceType.Internet)
                    : query.Where(bi => bi.DataSource == null || bi.DataSource.SourceType != DataSourceType.Internet);
            }

            if (status.HasValue)
                query = query.Where(bi => bi.ReviewStatus == status.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                query = query.Where(bi =>
                    (bi.SourceBearing != null && bi.SourceBearing.PartNumber.Contains(kw)) ||
                    (bi.TargetBearing != null && bi.TargetBearing.PartNumber.Contains(kw)));
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .Include(bi => bi.SourceBearing).ThenInclude(b => b!.Brand)
                .Include(bi => bi.SourceBearing).ThenInclude(b => b!.BearingTypeNavigation)
                .Include(bi => bi.TargetBearing).ThenInclude(b => b!.Brand)
                .Include(bi => bi.TargetBearing).ThenInclude(b => b!.BearingTypeNavigation)
                .OrderByDescending(bi => bi.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, total);
        }

        public async Task<(List<BearingInterchange> Items, int Total)> GetPendingAsync(
            int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.BearingInterchanges
                .Where(bi => bi.ReviewStatus == InterchangeReviewStatus.Pending);

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .Include(bi => bi.SourceBearing).ThenInclude(b => b!.Brand)
                .Include(bi => bi.SourceBearing).ThenInclude(b => b!.BearingTypeNavigation)
                .Include(bi => bi.TargetBearing).ThenInclude(b => b!.Brand)
                .Include(bi => bi.TargetBearing).ThenInclude(b => b!.BearingTypeNavigation)
                .OrderBy(bi => bi.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, total);
        }

        public async Task<List<BearingInterchange>> GetByProposerAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.BearingInterchanges
                .Include(bi => bi.SourceBearing).ThenInclude(b => b!.Brand)
                .Include(bi => bi.SourceBearing).ThenInclude(b => b!.BearingTypeNavigation)
                .Include(bi => bi.TargetBearing).ThenInclude(b => b!.Brand)
                .Include(bi => bi.TargetBearing).ThenInclude(b => b!.BearingTypeNavigation)
                .Where(bi => bi.ProposedByMerchantId == merchantId)
                .OrderByDescending(bi => bi.UpdatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.BearingInterchanges.CountAsync(bi => bi.IsActive, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.BearingInterchanges
                .CountAsync(bi => bi.IsActive && bi.ReviewStatus == InterchangeReviewStatus.Pending, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<int> GetCountSinceBySourceAsync(DateTime since, bool isInternet, CancellationToken cancellationToken = default)
        {
            // 改动说明（v1.50.0 来源拆分统计）：互联网=DataSource.SourceType 为 Internet（Sync 批量），
            //   人工=非互联网（平台维护 Manual + 商家报候选 UserSubmitted）
            var query = _context.BearingInterchanges.Where(bi => bi.IsActive && bi.CreatedAt >= since);
            query = isInternet
                ? query.Where(bi => bi.DataSource != null && bi.DataSource.SourceType == DataSourceType.Internet)
                : query.Where(bi => bi.DataSource == null || bi.DataSource.SourceType != DataSourceType.Internet);
            return await query.CountAsync(cancellationToken);
        }
    }
}
