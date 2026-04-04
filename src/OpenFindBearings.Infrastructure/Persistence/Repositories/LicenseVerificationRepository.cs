using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    public class LicenseVerificationRepository : ILicenseVerificationRepository
    {
        private readonly ApplicationDbContext _context;

        public LicenseVerificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<LicenseVerification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.LicenseVerifications
                .Include(l => l.Merchant)
                .Include(l => l.Submitter)
                .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        }

        public async Task<PagedResult<LicenseVerification>> GetPendingAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.LicenseVerifications
                .Where(l => l.Status == LicenseVerificationStatus.Pending)
                .OrderByDescending(l => l.SubmittedAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<LicenseVerification>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<LicenseVerification>> GetByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.LicenseVerifications
                .Where(l => l.MerchantId == merchantId)
                .OrderByDescending(l => l.SubmittedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(LicenseVerification verification, CancellationToken cancellationToken = default)
        {
            await _context.LicenseVerifications.AddAsync(verification, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(LicenseVerification verification, CancellationToken cancellationToken = default)
        {
            _context.LicenseVerifications.Update(verification);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
