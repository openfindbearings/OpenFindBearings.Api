using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Specifications;
using OpenFindBearings.Infrastructure.Persistence.Data;

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
                .Include(m => m.Staff)
                .Include(m => m.MerchantBearings)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }

        /// <summary>
        /// 根据用户ID获取商家（通过员工关联）
        /// </summary>
        public async Task<Merchant?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Merchants
                .Include(m => m.Staff)
                .Where(m => m.Staff.Any(s => s.Id == userId))
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// 商家搜索
        /// </summary>
        public async Task<PagedResult<Merchant>> SearchAsync(MerchantSearchParams searchParams, CancellationToken cancellationToken = default)
        {
            var query = _context.Merchants.AsNoTracking();

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

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
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

        // 检查名称是否存在
        public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _context.Merchants
                .AnyAsync(m => m.Name == name, cancellationToken);
        }

        // 获取总数
        public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Merchants.CountAsync(cancellationToken);
        }

        public async Task AddAsync(Merchant merchant, CancellationToken cancellationToken = default)
        {
            await _context.Merchants.AddAsync(merchant, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Merchant merchant, CancellationToken cancellationToken = default)
        {
            _context.Merchants.Update(merchant);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
