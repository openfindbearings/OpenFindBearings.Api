using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 轴承类型仓储实现
    /// </summary>
    public class BearingTypeRepository : IBearingTypeRepository
    {
        private readonly ApplicationDbContext _context;

        public BearingTypeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BearingType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.BearingTypes
                .FirstOrDefaultAsync(bt => bt.Id == id && bt.IsActive, cancellationToken);
        }

        public async Task<BearingType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _context.BearingTypes
                .FirstOrDefaultAsync(bt => bt.Code == code && bt.IsActive, cancellationToken);
        }

        public async Task<List<BearingType>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.BearingTypes
                .Where(bt => bt.IsActive)
                .OrderBy(bt => bt.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(BearingType bearingType, CancellationToken cancellationToken = default)
        {
            await _context.BearingTypes.AddAsync(bearingType, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(BearingType bearingType, CancellationToken cancellationToken = default)
        {
            _context.BearingTypes.Update(bearingType);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
