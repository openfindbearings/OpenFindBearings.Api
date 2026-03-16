using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    public class CorrectionRequestRepository : ICorrectionRequestRepository
    {
        private readonly AppDbContext _context;

        public CorrectionRequestRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CorrectionRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.CorrectionRequests
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<List<CorrectionRequest>> GetPendingAsync(string? entityType = null, CancellationToken cancellationToken = default)
        {
            var query = _context.CorrectionRequests
                .Where(c => c.Status == CorrectionStatus.Pending);

            if (!string.IsNullOrEmpty(entityType))
            {
                query = query.Where(c => c.EntityType == entityType);
            }

            return await query
                .OrderByDescending(c => c.SubmittedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<CorrectionRequest>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.CorrectionRequests
                .Where(c => c.SubmittedBy == userId)
                .OrderByDescending(c => c.SubmittedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(CorrectionRequest correction, CancellationToken cancellationToken = default)
        {
            await _context.CorrectionRequests.AddAsync(correction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(CorrectionRequest correction, CancellationToken cancellationToken = default)
        {
            _context.CorrectionRequests.Update(correction);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
