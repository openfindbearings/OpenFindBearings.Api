using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Specifications;
using OpenFindBearings.Infrastructure.Persistence.Data;
using MerchantStatus = OpenFindBearings.Domain.Enums.MerchantStatus;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    public class MerchantRepository : IMerchantRepository
    {
        private readonly ApplicationDbContext _context;

        public MerchantRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Merchant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Merchants
                .Include(m => m.MerchantBearings)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }

        // 改动说明：移除 GetByUserIdAsync（依赖已废弃的 Merchant.Staff / User.MerchantId 单值关系）；
        //   按用户定位商户统一走成员表（GetMerchantByUserId 查询处理器），全项目此方法零调用者

        /// <summary>
        /// 商家搜索
        /// </summary>
        public async Task<PagedResult<Merchant>> SearchAsync(MerchantSearchParams searchParams, CancellationToken cancellationToken = default)
        {
            if (searchParams.Page < 1) searchParams.Page = 1;
            if (searchParams.PageSize < 1) searchParams.PageSize = 20;
            if (searchParams.PageSize > 100) searchParams.PageSize = 100;

            var query = _context.Merchants.AsNoTracking();

            if (searchParams.IsActive.HasValue)
                query = query.Where(m => m.IsActive == searchParams.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(searchParams.Keyword))
                query = query.Where(m =>
                    m.Name.Contains(searchParams.Keyword) ||
                    (m.CompanyName != null && m.CompanyName.Contains(searchParams.Keyword)));

            if (searchParams.Type.HasValue)
                query = query.Where(m => m.Type == searchParams.Type);

            if (!string.IsNullOrWhiteSpace(searchParams.City))
                query = query.Where(m =>
                    m.Contact.Address != null &&
                    m.Contact.Address.Contains(searchParams.City));

            if (searchParams.VerifiedOnly.HasValue && searchParams.VerifiedOnly.Value)
                query = query.Where(m => m.IsVerified);

            if (searchParams.Status.HasValue)
                query = query.Where(m => m.Status == searchParams.Status.Value);
            else
                // 修复 B6：默认排除提名草稿（Draft）商户——资料不全、未提交审核，
                // 不应泄露到 C 端搜索与 Admin 入驻审核列表；显式按状态过滤时不受影响
                query = query.Where(m => m.Status != MerchantStatus.Draft);

            if (searchParams.ExcludeCrawler == true)
                query = query.Where(m =>
                    m.DataSource == null ||
                    m.DataSource.SourceType != Domain.Enums.DataSourceType.Crawler);

            var totalCount = await query.CountAsync(cancellationToken);

            // 改动说明：所有排序分支都以"已认证/入驻优先"为主排序（OrderByDescending(IsVerified)），
            // 用户选的 名称/在售数 仅作次排序，保证入驻商家始终排在前面。
            var desc = searchParams.SortOrder?.ToLower() == "desc";
            var ordered = (searchParams.SortBy?.ToLower()) switch
            {
                "name" => desc
                    ? query.OrderByDescending(m => m.IsVerified).ThenByDescending(m => m.Name)
                    : query.OrderByDescending(m => m.IsVerified).ThenBy(m => m.Name),
                "productcount" => desc
                    ? query.OrderByDescending(m => m.IsVerified).ThenBy(m => m.ProductCount)
                    : query.OrderByDescending(m => m.IsVerified).ThenByDescending(m => m.ProductCount),
                _ => query.OrderByDescending(m => m.IsVerified).ThenBy(m => m.Name)
            };

            var items = await ordered
                .Skip((searchParams.Page - 1) * searchParams.PageSize)
                .Take(searchParams.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Merchant>
            {
                Items = items,
                TotalCount = totalCount,
                Page = searchParams.Page,
                PageSize = searchParams.PageSize
            };
        }

        /// <summary>
        /// 入驻发现搜索（v2.9.0 由"仅可认领池"升级为全量匹配）：返回关键词命中的非草稿商户
        /// （含已入驻/审核中/已认证），认领可行性判定（无成员/无提名锁定）由查询处理器计算，
        /// 让用户在搜索阶段就看到"已被入驻"，避免重复新建
        /// </summary>
        public async Task<PagedResult<Merchant>> GetDiscoverableAsync(string? keyword, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            // 改动说明：只排除草稿（Draft 对搜索不可见）；是否可认领的三条件
            //   （未认证/无在职成员/无进行中提名）移到 handler 逐条计算并回传标记
            var query = _context.Merchants.AsNoTracking()
                .Where(m => m.Status != MerchantStatus.Draft);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(m =>
                    m.Name.Contains(keyword) ||
                    (m.CompanyName != null && m.CompanyName.Contains(keyword)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(m => m.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Merchant>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        // 检查名称是否存在
        public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _context.Merchants
                .AnyAsync(m => m.Name == name, cancellationToken);
        }

        // 根据名称精确获取商家
        // 修复 B6：排除提名草稿（Draft）商户——Sync 按名称匹配合并时不得覆盖未生效的草稿资料
        public async Task<Merchant?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _context.Merchants
                .FirstOrDefaultAsync(m => m.Name == name && m.Status != MerchantStatus.Draft, cancellationToken);
        }

        // 根据统一社会信用代码精确获取非草稿商家（入驻查重用，代码优先于名称，草稿不计）
        public async Task<Merchant?> GetByCreditCodeAsync(string creditCode, CancellationToken cancellationToken = default)
        {
            return await _context.Merchants
                .FirstOrDefaultAsync(
                    m => m.UnifiedSocialCreditCode == creditCode && m.Status != MerchantStatus.Draft,
                    cancellationToken);
        }

        // 获取总数
        // 修复 B6：统计口径排除提名草稿（Draft），Admin 仪表盘商户总数只含已提交/生效商户
        public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Merchants.CountAsync(m => m.Status != MerchantStatus.Draft, cancellationToken);
        }

        public async Task AddAsync(Merchant merchant, CancellationToken cancellationToken = default)
        {
            await _context.Merchants.AddAsync(merchant, cancellationToken);
            
        }

        public async Task<int> GetCountSinceAsync(DateTime since, CancellationToken cancellationToken = default)
        {
            return await _context.Merchants
                .Where(m => m.IsActive && m.CreatedAt >= since)
                .CountAsync(cancellationToken);
        }

        public async Task<int> GetVerifiedCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Merchants
                .Where(m => m.IsActive && m.IsVerified)
                .CountAsync(cancellationToken);
        }

        public async Task<int> GetPendingApplicationCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Merchants
                .Where(m => m.IsActive &&
                            m.Status == MerchantStatus.Pending &&
                            (m.DataSource == null || m.DataSource.SourceType != Domain.Enums.DataSourceType.Crawler))
                .CountAsync(cancellationToken);
        }

        public async Task<Dictionary<MerchantType, int>> GetTypeDistributionAsync(CancellationToken cancellationToken = default)
        {
            // 修复 B6：类型分布统计排除提名草稿（Draft）
            return await _context.Merchants
                .Where(m => m.IsActive && m.Status != MerchantStatus.Draft)
                .GroupBy(m => m.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count, cancellationToken);
        }

        public async Task UpdateAsync(Merchant merchant, CancellationToken cancellationToken = default)
        {
            _context.Merchants.Update(merchant);
        }

        public async Task<Merchant?> GetByIdIgnoringFilterAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Merchants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }

        public async Task RemoveAsync(Merchant merchant, CancellationToken cancellationToken = default)
        {
            _context.Merchants.Remove(merchant);
        }
    }
}
