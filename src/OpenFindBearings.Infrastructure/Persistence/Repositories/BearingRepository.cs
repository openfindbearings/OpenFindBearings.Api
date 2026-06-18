using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Specifications;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    public class BearingRepository : IBearingRepository
    {
        private readonly ApplicationDbContext _context;

        public BearingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Bearing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Bearings
                .Include(b => b.Brand)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        }

        public async Task<Bearing?> GetByPartNumberAsync(string partNumber, CancellationToken cancellationToken = default)
        {
            return await _context.Bearings
                .Include(b => b.Brand)
                .FirstOrDefaultAsync(b => b.PartNumber == partNumber, cancellationToken);
        }

        public async Task<PagedResult<Bearing>> SearchAsync(BearingSearchParams searchParams, CancellationToken cancellationToken = default)
        {
            if (searchParams.Page < 1) searchParams.Page = 1;
            if (searchParams.PageSize < 1) searchParams.PageSize = 20;
            if (searchParams.PageSize > 100) searchParams.PageSize = 100;

            var query = _context.Bearings
                .Include(b => b.Brand)
                .AsNoTracking()
                .Where(b => searchParams.IsActive == null || b.IsActive == searchParams.IsActive.Value);

            // 现行代号搜索
            if (!string.IsNullOrWhiteSpace(searchParams.PartNumber))
            {
                query = query.Where(b => b.PartNumber.Contains(searchParams.PartNumber));
            }

            // 曾用代号搜索
            if (!string.IsNullOrWhiteSpace(searchParams.OldNumber))
            {
                query = query.Where(b => b.OldNumber != null && b.OldNumber.Contains(searchParams.OldNumber));
            }

            // 关键词搜索
            if (!string.IsNullOrWhiteSpace(searchParams.Keyword))
            {
                query = query.Where(b =>
                    b.PartNumber.Contains(searchParams.Keyword) ||
                    (b.OldNumber != null && b.OldNumber.Contains(searchParams.Keyword)) ||
                    (b.Description != null && b.Description.Contains(searchParams.Keyword)));
            }

            // 内径范围
            if (searchParams.MinInnerDiameter.HasValue)
            {
                query = query.Where(b => b.Dimensions.InnerDiameter >= searchParams.MinInnerDiameter.Value);
            }
            if (searchParams.MaxInnerDiameter.HasValue)
            {
                query = query.Where(b => b.Dimensions.InnerDiameter <= searchParams.MaxInnerDiameter.Value);
            }

            // 外径范围
            if (searchParams.MinOuterDiameter.HasValue)
            {
                query = query.Where(b => b.Dimensions.OuterDiameter >= searchParams.MinOuterDiameter.Value);
            }
            if (searchParams.MaxOuterDiameter.HasValue)
            {
                query = query.Where(b => b.Dimensions.OuterDiameter <= searchParams.MaxOuterDiameter.Value);
            }

            // 宽度范围
            if (searchParams.MinWidth.HasValue)
            {
                query = query.Where(b => b.Dimensions.Width >= searchParams.MinWidth.Value);
            }
            if (searchParams.MaxWidth.HasValue)
            {
                query = query.Where(b => b.Dimensions.Width <= searchParams.MaxWidth.Value);
            }

            // 品牌筛选
            if (searchParams.BrandId.HasValue)
            {
                query = query.Where(b => b.BrandId == searchParams.BrandId.Value);
            }

            // ✅ 轴承类型筛选 - 直接使用 BearingTypeId
            if (searchParams.BearingTypeId.HasValue)
            {
                query = query.Where(b => b.BearingTypeId == searchParams.BearingTypeId.Value);
            }

            // 产地筛选
            if (!string.IsNullOrWhiteSpace(searchParams.OriginCountry))
            {
                query = query.Where(b => b.OriginCountry == searchParams.OriginCountry);
            }

            // 类别筛选
            if (searchParams.Category.HasValue)
            {
                query = query.Where(b => b.Category == searchParams.Category.Value);
            }

            // 是否标准轴承
            if (searchParams.IsStandard.HasValue)
            {
                query = query.Where(b => b.IsStandard == searchParams.IsStandard.Value);
            }

            // 获取总数
            var totalCount = await query.CountAsync(cancellationToken);

            // 排序
            query = (searchParams.SortBy?.ToLower()) switch
            {
                "partnumber" => searchParams.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(b => b.PartNumber)
                    : query.OrderBy(b => b.PartNumber),

                "innerdiameter" => searchParams.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(b => b.Dimensions.InnerDiameter)
                    : query.OrderBy(b => b.Dimensions.InnerDiameter),

                "outerdiameter" => searchParams.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(b => b.Dimensions.OuterDiameter)
                    : query.OrderBy(b => b.Dimensions.OuterDiameter),

                "width" => searchParams.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(b => b.Dimensions.Width)
                    : query.OrderBy(b => b.Dimensions.Width),

                "viewcount" => searchParams.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(b => b.ViewCount)
                    : query.OrderBy(b => b.ViewCount),

                _ => searchParams.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(b => b.PartNumber)
                    : query.OrderBy(b => b.PartNumber)
            };

            // 分页
            var items = await query
                .Skip((searchParams.Page - 1) * searchParams.PageSize)
                .Take(searchParams.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Bearing>
            {
                Items = items,
                TotalCount = totalCount,
                Page = searchParams.Page,
                PageSize = searchParams.PageSize
            };
        }

        public async Task<int> GetTotalCountAsync(BearingSearchParams searchParams, CancellationToken cancellationToken = default)
        {
            var query = _context.Bearings.Where(b => b.IsActive);

            if (!string.IsNullOrWhiteSpace(searchParams.PartNumber))
                query = query.Where(b => b.PartNumber.Contains(searchParams.PartNumber));

            if (!string.IsNullOrWhiteSpace(searchParams.OldNumber))
                query = query.Where(b => b.OldNumber != null && b.OldNumber.Contains(searchParams.OldNumber));

            if (!string.IsNullOrWhiteSpace(searchParams.Keyword))
                query = query.Where(b =>
                    b.PartNumber.Contains(searchParams.Keyword) ||
                    (b.OldNumber != null && b.OldNumber.Contains(searchParams.Keyword)));

            if (!string.IsNullOrWhiteSpace(searchParams.OriginCountry))
                query = query.Where(b => b.OriginCountry == searchParams.OriginCountry);

            if (searchParams.Category.HasValue)
                query = query.Where(b => b.Category == searchParams.Category.Value);

            if (searchParams.MinInnerDiameter.HasValue)
                query = query.Where(b => b.Dimensions.InnerDiameter >= searchParams.MinInnerDiameter.Value);

            if (searchParams.MaxInnerDiameter.HasValue)
                query = query.Where(b => b.Dimensions.InnerDiameter <= searchParams.MaxInnerDiameter.Value);

            if (searchParams.MinOuterDiameter.HasValue)
                query = query.Where(b => b.Dimensions.OuterDiameter >= searchParams.MinOuterDiameter.Value);

            if (searchParams.MaxOuterDiameter.HasValue)
                query = query.Where(b => b.Dimensions.OuterDiameter <= searchParams.MaxOuterDiameter.Value);

            if (searchParams.MinWidth.HasValue)
                query = query.Where(b => b.Dimensions.Width >= searchParams.MinWidth.Value);

            if (searchParams.MaxWidth.HasValue)
                query = query.Where(b => b.Dimensions.Width <= searchParams.MaxWidth.Value);

            if (searchParams.BrandId.HasValue)
                query = query.Where(b => b.BrandId == searchParams.BrandId.Value);

            // ✅ 轴承类型筛选 - 直接使用 BearingTypeId
            if (searchParams.BearingTypeId.HasValue)
            {
                query = query.Where(b => b.BearingTypeId == searchParams.BearingTypeId.Value);
            }

            if (searchParams.IsStandard.HasValue)
                query = query.Where(b => b.IsStandard == searchParams.IsStandard.Value);

            return await query.CountAsync(cancellationToken);
        }

        public async Task AddAsync(Bearing bearing, CancellationToken cancellationToken = default)
        {
            await _context.Bearings.AddAsync(bearing, cancellationToken);
        }

        public async Task UpdateAsync(Bearing bearing, CancellationToken cancellationToken = default)
        {
            _context.Bearings.Update(bearing);
        }

        public async Task<bool> ExistsByPartNumberAsync(string partNumber, CancellationToken cancellationToken = default)
        {
            return await _context.Bearings
                .AnyAsync(b => b.PartNumber == partNumber, cancellationToken);
        }

        public async Task<IEnumerable<Bearing>> GetHotBearingsAsync(int count, CancellationToken cancellationToken = default)
        {
            return await _context.Bearings
                .Include(b => b.Brand)
                .Where(b => b.IsActive)
                .OrderByDescending(b => b.ViewCount)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Bearing>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Bearings
                .Include(b => b.Brand)
                .Where(b => b.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task<Dictionary<Guid, int>> GetBearingCountByTypeAsync(CancellationToken cancellationToken = default)
        {
            // 现在可以用 BearingTypeId 分组了
            return await _context.Bearings
                .Where(b => b.IsActive)
                .GroupBy(b => b.BearingTypeId)
                .Select(g => new { BearingTypeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BearingTypeId, x => x.Count, cancellationToken);
        }

        public async Task<Dictionary<Guid, int>> GetBearingCountByBrandAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Bearings
                .Where(b => b.IsActive)
                .GroupBy(b => b.BrandId)
                .Select(g => new { BrandId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BrandId, x => x.Count, cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var bearing = await GetByIdAsync(id, cancellationToken);
            if (bearing != null)
            {
                bearing.Deactivate();
                await UpdateAsync(bearing, cancellationToken);
            }
        }

        public async Task<Bearing?> GetByIdIgnoringFilterAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Bearings
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        }

        public async Task RemoveAsync(Bearing bearing, CancellationToken cancellationToken = default)
        {
            _context.Bearings.Remove(bearing);
        }
    }
}
