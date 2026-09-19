using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    public class MerchantDocumentRepository : IMerchantDocumentRepository
    {
        private readonly ApplicationDbContext _context;

        public MerchantDocumentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<MerchantDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.MerchantDocuments
                .Include(l => l.Merchant)
                .Include(l => l.Submitter)
                .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        }

        public async Task<PagedResult<MerchantDocument>> GetPendingAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.MerchantDocuments
                .Where(l => l.Status == DocumentStatus.Pending)
                .OrderByDescending(l => l.SubmittedAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<MerchantDocument>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<MerchantDocument>> GetByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.MerchantDocuments
                .Where(l => l.MerchantId == merchantId)
                .OrderByDescending(l => l.SubmittedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(MerchantDocument document, CancellationToken cancellationToken = default)
        {
            await _context.MerchantDocuments.AddAsync(document, cancellationToken);
        }

        public async Task UpdateAsync(MerchantDocument document, CancellationToken cancellationToken = default)
        {
            _context.MerchantDocuments.Update(document);
        }

        public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.MerchantDocuments
                .CountAsync(l => l.Status == DocumentStatus.Pending, cancellationToken);
        }
    }
}
