using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

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
                    .ThenInclude(b => b.BearingType)
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
                    .ThenInclude(b => b.BearingType)
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
            _context.BearingInterchanges.Update(interchange);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var interchange = await GetByIdAsync(id, cancellationToken);
            if (interchange != null)
            {
                interchange.Deactivate();
            }
        }
    }
}
