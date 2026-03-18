using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;
using OpenFindBearings.Domain.Specifications;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    public class MerchantRepository : IMerchantRepository
    {
        private readonly AppDbContext _context;

        public MerchantRepository(AppDbContext context)
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

        public async Task<IEnumerable<Merchant>> SearchAsync(MerchantSearchParams searchParams, CancellationToken cancellationToken = default)
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

            return await query
                .Skip((searchParams.Page - 1) * searchParams.PageSize)
                .Take(searchParams.PageSize)
                .ToListAsync(cancellationToken);
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
