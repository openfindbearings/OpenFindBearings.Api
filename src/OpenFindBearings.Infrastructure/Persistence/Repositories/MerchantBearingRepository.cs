using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    public class MerchantBearingRepository : IMerchantBearingRepository
    {
        private readonly AppDbContext _context;

        public MerchantBearingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<MerchantBearing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.MerchantBearings
                .Include(mp => mp.Merchant)
                .Include(mp => mp.Bearing)
                .FirstOrDefaultAsync(mp => mp.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<MerchantBearing>> GetByBearingAsync(Guid bearingId, CancellationToken cancellationToken = default)
        {
            var results = await _context.MerchantBearings
                .Include(mp => mp.Merchant)  // 必须 Include 商家信息
                .Where(mp => mp.BearingId == bearingId && mp.IsActive)
                .ToListAsync(cancellationToken);

            return results;
        }

        public async Task<IEnumerable<MerchantBearing>> GetByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.MerchantBearings
                .Include(mp => mp.Bearing)
                    .ThenInclude(b => b.Brand)
                .Include(mp => mp.Bearing)
                    .ThenInclude(b => b.BearingType)
                .Where(mp => mp.MerchantId == merchantId && mp.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(MerchantBearing merchantBearing, CancellationToken cancellationToken = default)
        {
            await _context.MerchantBearings.AddAsync(merchantBearing, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(MerchantBearing merchantBearing, CancellationToken cancellationToken = default)
        {
            _context.MerchantBearings.Update(merchantBearing);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var merchantBearing = await GetByIdAsync(id, cancellationToken);
            if (merchantBearing != null)
            {
                _context.MerchantBearings.Remove(merchantBearing);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
