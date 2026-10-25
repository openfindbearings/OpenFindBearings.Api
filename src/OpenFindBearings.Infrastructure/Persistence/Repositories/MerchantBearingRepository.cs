using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Specifications;
using OpenFindBearings.Infrastructure.Persistence.Data;
using OpenFindBearings.Infrastructure.Persistence.Extensions;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 商家轴承关联仓储实现
    /// </summary>
    public class MerchantBearingRepository : IMerchantBearingRepository
    {
        private readonly ApplicationDbContext _context;

        public MerchantBearingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<MerchantBearing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.MerchantBearings
                .Include(mb => mb.Merchant)
                .Include(mb => mb.Bearing)
                .FirstOrDefaultAsync(mb => mb.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<MerchantBearing>> GetByBearingAsync(Guid bearingId, CancellationToken cancellationToken = default)
        {
            return await _context.MerchantBearings
                .Include(mb => mb.Merchant)
                .Where(mb => mb.BearingId == bearingId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<MerchantBearing>> GetByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.MerchantBearings
                .Include(mb => mb.Bearing)
                    .ThenInclude(b => b.Brand)
                .Include(mb => mb.Bearing)
                    .ThenInclude(b => b.BearingTypeNavigation)
                .Where(mb => mb.MerchantId == merchantId)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 获取商家在售的轴承列表
        /// </summary>
        public async Task<IEnumerable<MerchantBearing>> GetOnSaleByMerchantAsync(
            Guid merchantId,
            CancellationToken cancellationToken = default)
        {
            return await _context.MerchantBearings
                .Include(mb => mb.Bearing)
                    .ThenInclude(b => b.Brand)
                .Include(mb => mb.Bearing)
                    .ThenInclude(b => b.BearingTypeNavigation)
                .Where(mb => mb.MerchantId == merchantId
                    && mb.ReviewStatus == ListingReviewStatus.Approved
                    && mb.SaleState == SaleState.OnSale)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 商家"公域可见"商品数量（v1.5.0 寻货比价"实力摘要"；v1.8.2 口径统一=审核通过 且（出售中/补货中）且未下架，轻量 Count 不加载全量）
        /// </summary>
        public Task<int> CountPubliclyVisibleAsync(Guid merchantId, CancellationToken cancellationToken = default)
            => _context.MerchantBearings.CountAsync(
                mb => mb.MerchantId == merchantId
                    && mb.ReviewStatus == ListingReviewStatus.Approved
                    && (mb.SaleState == SaleState.OnSale || mb.SaleState == SaleState.Restocking)
                    && mb.IsActive, cancellationToken);

        /// <summary>
        /// 获取商家"可匹配寻货"的关联列表（v1.8.1 寻货聚合口径统一）
        /// 口径 = 审核通过 且（出售中 或 补货中）且未停用，与 SourcingMatcher 匹配口径一致
        /// </summary>
        public async Task<IEnumerable<MerchantBearing>> GetMatchableByMerchantAsync(
            Guid merchantId,
            CancellationToken cancellationToken = default)
        {
            return await _context.MerchantBearings
                .Include(mb => mb.Bearing)
                    .ThenInclude(b => b.Brand)
                .Include(mb => mb.Bearing)
                    .ThenInclude(b => b.BearingTypeNavigation)
                .Where(mb => mb.MerchantId == merchantId
                    && mb.ReviewStatus == ListingReviewStatus.Approved
                    && (mb.SaleState == SaleState.OnSale || mb.SaleState == SaleState.Restocking)
                    && mb.IsActive)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 获取待审核的关联列表（v2.15.0：审核中）
        /// </summary>
        public async Task<IEnumerable<MerchantBearing>> GetPendingApprovalAsync(CancellationToken cancellationToken = default)
        {
            return await _context.MerchantBearings
                .Include(mb => mb.Merchant)
                .Include(mb => mb.Bearing)
                .Where(mb => mb.ReviewStatus == ListingReviewStatus.Pending)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 检查是否存在关联
        /// </summary>
        public async Task<bool> ExistsAsync(Guid merchantId, Guid bearingId, CancellationToken cancellationToken = default)
        {
            return await _context.MerchantBearings
                .AnyAsync(mb => mb.MerchantId == merchantId && mb.BearingId == bearingId, cancellationToken);
        }

        /// <summary>
        /// 检查轴承是否属于商家
        /// </summary>
        public async Task<bool> IsOwnedByMerchantAsync(Guid bearingId, Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.MerchantBearings
                .AnyAsync(mb => mb.BearingId == bearingId && mb.MerchantId == merchantId, cancellationToken);
        }

        public async Task AddAsync(MerchantBearing merchantBearing, CancellationToken cancellationToken = default)
        {
            await _context.MerchantBearings.AddAsync(merchantBearing, cancellationToken);
            
        }

        /// <summary>
        /// 批量添加关联
        /// </summary>
        public async Task AddRangeAsync(
            IEnumerable<MerchantBearing> merchantBearings,
            CancellationToken cancellationToken = default)
        {
            await _context.MerchantBearings.AddRangeAsync(merchantBearings, cancellationToken);
            
        }

        public async Task UpdateAsync(MerchantBearing merchantBearing, CancellationToken cancellationToken = default)
        {
            _context.MarkModified(merchantBearing);
            
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var merchantBearing = await GetByIdAsync(id, cancellationToken);
            if (merchantBearing != null)
            {
                _context.MerchantBearings.Remove(merchantBearing);
                
            }
        }

        public async Task<PagedResult<MerchantBearing>> GetMerchantBearingsPagedAsync(
            Guid merchantId, bool? onlyOnSale, bool? pendingOnly, ListingReviewStatus? reviewStatus, DataSourceType? dataSource,
            int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.MerchantBearings
                .Include(mb => mb.Bearing)
                    .ThenInclude(b => b.Brand)
                .Include(mb => mb.Bearing)
                    .ThenInclude(b => b.BearingTypeNavigation)
                .Where(mb => mb.MerchantId == merchantId);

            // v2.15.0 状态统一：在售=审核通过且在售/补货中（公域可见口径）；待审=审核中
            if (onlyOnSale == true)
                query = query.Where(mb => mb.ReviewStatus == ListingReviewStatus.Approved
                    && (mb.SaleState == SaleState.OnSale || mb.SaleState == SaleState.Restocking));

            if (pendingOnly == true)
                query = query.Where(mb => mb.ReviewStatus == ListingReviewStatus.Pending);

            // v2.15.0 状态统一：审核位精确过滤（仓库中/审核中/已通过/已驳回）
            if (reviewStatus.HasValue)
                query = query.Where(mb => mb.ReviewStatus == reviewStatus.Value);

            if (dataSource.HasValue)
                query = query.Where(mb => mb.DataSourceType == dataSource.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(mb => mb.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<MerchantBearing>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<int> GetPendingApprovalCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.MerchantBearings
                .CountAsync(mb => mb.ReviewStatus == ListingReviewStatus.Pending, cancellationToken);
        }

        /// <summary>删除商户全部商品关联（ExecuteDelete 单 SQL，接管重置用）</summary>
        public async Task<int> DeleteByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<MerchantBearing>()
                .Where(mb => mb.MerchantId == merchantId)
                .ExecuteDeleteAsync(cancellationToken);
        }

/// <inheritdoc/>
        public async Task<int> CountCreatedSinceAsync(Guid merchantId, DateTime sinceUtc, CancellationToken cancellationToken = default)
        {
            // v2.6.0 集体任务 products 指标：窗口内新上架的产品关联（含待审核，鼓励供给动作本身）
            return await _context.Set<MerchantBearing>()
                .CountAsync(mb => mb.MerchantId == merchantId && mb.IsActive && mb.CreatedAt >= sinceUtc, cancellationToken);
        }

        /// <summary>
        /// 商城货源流分页搜索（v2.14.0 商城导购）：以在售关联为行，DB 级过滤/排序/分页。
        /// 不指定商家/轴承即"全站在售货源"；关键词匹配型号/旧代号/描述/类型名/品牌名。
        /// </summary>
        public async Task<PagedResult<MerchantBearing>> SearchMallFeedAsync(
            MallFeedSearchParams p, CancellationToken cancellationToken = default)
        {
            // 分页护栏：与 BearingRepository.SearchAsync 同口径
            if (p.Page < 1) p.Page = 1;
            if (p.PageSize < 1) p.PageSize = 20;
            if (p.PageSize > 100) p.PageSize = 100;

            var query = _context.MerchantBearings
                .Include(mb => mb.Merchant)
                .Include(mb => mb.Bearing)
                    .ThenInclude(b => b.Brand)
                .Include(mb => mb.Bearing)
                    .ThenInclude(b => b.BearingTypeNavigation)
                .AsNoTracking()
                // v2.15.0 状态统一：商城货源流仅出"审核通过"的商品（未审/驳回不进公域）
                .Where(mb => mb.IsActive && mb.ReviewStatus == ListingReviewStatus.Approved)
                // 改动说明（v2.21.1 商城口径收敛）：补商家维度过滤——此前只过滤商品，导致未认证商家、
                //   爬虫商家（创建即 Pending、从不认证）乃至已停用/已关店商家的商品都会出现在商城。
                //   商城定位"可联系、可报价"的成交型货源，故商家必须有效（Active + 未停用）；
                //   是否进一步限定"已认证"由 VerifiedOnly 控制，默认 true。
                .Where(mb => mb.Merchant != null && mb.Merchant.IsActive
                    && mb.Merchant.Status == MerchantStatus.Active);

            // 认证过滤：商城默认只出认证商家货源，互联网来源/未认证数据只在型号详情参考段出现
            if (p.VerifiedOnly)
                query = query.Where(mb => mb.Merchant!.IsVerified);

            // 在售过滤（商城货源流默认 true：仅出售中）
            if (p.IsOnSale == true)
                query = query.Where(mb => mb.SaleState == SaleState.OnSale);

            // 关键词：型号/旧代号/描述/类型名/品牌名
            if (!string.IsNullOrWhiteSpace(p.Keyword))
            {
                var kw = p.Keyword.Trim();
                query = query.Where(mb =>
                    mb.Bearing!.PartNumber.Contains(kw) ||
                    (mb.Bearing.OldNumber != null && mb.Bearing.OldNumber.Contains(kw)) ||
                    (mb.Bearing.Description != null && mb.Bearing.Description.Contains(kw)) ||
                    mb.Bearing.BearingType.Contains(kw) ||
                    (mb.Bearing.Brand != null && mb.Bearing.Brand.Name.Contains(kw)));
            }

            // 品牌 / 类型 / 类别
            if (p.BrandId.HasValue)
                query = query.Where(mb => mb.Bearing!.BrandId == p.BrandId.Value);
            if (p.BearingTypeId.HasValue)
                query = query.Where(mb => mb.Bearing!.BearingTypeId == p.BearingTypeId.Value);
            if (p.Category.HasValue)
                query = query.Where(mb => mb.Bearing!.Category == p.Category.Value);

            // 尺寸范围（内径/外径/宽度）
            if (p.MinInnerDiameter.HasValue)
                query = query.Where(mb => mb.Bearing!.Dimensions.InnerDiameter >= p.MinInnerDiameter.Value);
            if (p.MaxInnerDiameter.HasValue)
                query = query.Where(mb => mb.Bearing!.Dimensions.InnerDiameter <= p.MaxInnerDiameter.Value);
            if (p.MinOuterDiameter.HasValue)
                query = query.Where(mb => mb.Bearing!.Dimensions.OuterDiameter >= p.MinOuterDiameter.Value);
            if (p.MaxOuterDiameter.HasValue)
                query = query.Where(mb => mb.Bearing!.Dimensions.OuterDiameter <= p.MaxOuterDiameter.Value);
            if (p.MinWidth.HasValue)
                query = query.Where(mb => mb.Bearing!.Dimensions.Width >= p.MinWidth.Value);
            if (p.MaxWidth.HasValue)
                query = query.Where(mb => mb.Bearing!.Dimensions.Width <= p.MaxWidth.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            // 排序：价格/浏览/创建时间（缺省创建时间倒序，与商家列表口径一致）
            query = p.SortBy?.ToLower() switch
            {
                "price" => p.SortOrder?.ToLower() == "asc"
                    ? query.OrderBy(mb => mb.NumericPrice)
                    : query.OrderByDescending(mb => mb.NumericPrice),
                "viewcount" => p.SortOrder?.ToLower() == "asc"
                    ? query.OrderBy(mb => mb.ViewCount)
                    : query.OrderByDescending(mb => mb.ViewCount),
                "created" => p.SortOrder?.ToLower() == "asc"
                    ? query.OrderBy(mb => mb.CreatedAt)
                    : query.OrderByDescending(mb => mb.CreatedAt),
                _ => query.OrderByDescending(mb => mb.CreatedAt)
            };

            var items = await query
                .Skip((p.Page - 1) * p.PageSize)
                .Take(p.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<MerchantBearing>
            {
                Items = items,
                TotalCount = totalCount,
                Page = p.Page,
                PageSize = p.PageSize
            };
        }
    }
}
