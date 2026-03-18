using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;
using OpenFindBearings.Domain.Specifications;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    public class BearingRepository : IBearingRepository
    {
        private readonly AppDbContext _context;

        public BearingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Bearing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Bearings
                .Include(b => b.BearingType)
                .Include(b => b.Brand)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        }

        public async Task<Bearing?> GetByPartNumberAsync(string partNumber, CancellationToken cancellationToken = default)
        {
            return await _context.Bearings
                .Include(b => b.BearingType)
                .Include(b => b.Brand)
                .FirstOrDefaultAsync(b => b.PartNumber == partNumber, cancellationToken);
        }

        public async Task<IEnumerable<Bearing>> SearchAsync(BearingSearchParams searchParams, CancellationToken cancellationToken = default)
        {
            var query = _context.Bearings
                .Include(b => b.BearingType)
                .Include(b => b.Brand)
                .AsNoTracking()
                .Where(b => b.IsActive);

            // 型号模糊搜索
            if (!string.IsNullOrWhiteSpace(searchParams.PartNumber))
            {
                query = query.Where(b => b.PartNumber.Contains(searchParams.PartNumber));
            }

            // 关键词搜索
            if (!string.IsNullOrWhiteSpace(searchParams.Keyword))
            {
                query = query.Where(b =>
                    b.PartNumber.Contains(searchParams.Keyword) ||
                    b.Name.Contains(searchParams.Keyword) ||
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

            // 类型筛选
            if (searchParams.BearingTypeId.HasValue)
            {
                query = query.Where(b => b.BearingTypeId == searchParams.BearingTypeId.Value);
            }

            // 排序
            query = (searchParams.SortBy?.ToLower()) switch
            {
                "innerdiameter" => searchParams.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(b => b.Dimensions.InnerDiameter)
                    : query.OrderBy(b => b.Dimensions.InnerDiameter),

                "outerdiameter" => searchParams.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(b => b.Dimensions.OuterDiameter)
                    : query.OrderBy(b => b.Dimensions.OuterDiameter),

                "width" => searchParams.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(b => b.Dimensions.Width)
                    : query.OrderBy(b => b.Dimensions.Width),

                _ => searchParams.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(b => b.PartNumber)  // 按型号降序
                    : query.OrderBy(b => b.PartNumber)            // 按型号升序（默认）
            };

            // 分页
            return await query
                .Skip((searchParams.Page - 1) * searchParams.PageSize)
                .Take(searchParams.PageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetTotalCountAsync(BearingSearchParams searchParams, CancellationToken cancellationToken = default)
        {
            var query = _context.Bearings
                .Where(b => b.IsActive);

            // 应用相同的过滤条件
            if (!string.IsNullOrWhiteSpace(searchParams.PartNumber))
                query = query.Where(b => b.PartNumber.Contains(searchParams.PartNumber));

            if (!string.IsNullOrWhiteSpace(searchParams.Keyword))
                query = query.Where(b =>
                    b.PartNumber.Contains(searchParams.Keyword) ||
                    b.Name.Contains(searchParams.Keyword));

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

            if (searchParams.BearingTypeId.HasValue)
                query = query.Where(b => b.BearingTypeId == searchParams.BearingTypeId.Value);

            return await query.CountAsync(cancellationToken);
        }

        public async Task AddAsync(Bearing bearing, CancellationToken cancellationToken = default)
        {
            await _context.Bearings.AddAsync(bearing, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Bearing bearing, CancellationToken cancellationToken = default)
        {
            _context.Bearings.Update(bearing);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(string partNumber, CancellationToken cancellationToken = default)
        {
            return await _context.Bearings
                .AnyAsync(b => b.PartNumber == partNumber, cancellationToken);
        }

        public async Task<IEnumerable<Bearing>> GetHotBearingsAsync(int count, CancellationToken cancellationToken = default)
        {
            return await _context.Bearings
                .Include(b => b.BearingType)
                .Include(b => b.Brand)
                .Where(b => b.IsActive)
                .OrderByDescending(b => b.ViewCount)  // 按浏览次数排序
                .Take(count)
                .ToListAsync(cancellationToken);
        }
    }
}
