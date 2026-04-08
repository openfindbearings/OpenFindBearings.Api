using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 品牌仓储实现
    /// </summary>
    public class BrandRepository : IBrandRepository
    {
        private readonly ApplicationDbContext _context;

        public BrandRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Brands
                .FirstOrDefaultAsync(b => b.Id == id && b.IsActive, cancellationToken);
        }

        public async Task<Brand?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _context.Brands
                .FirstOrDefaultAsync(b => b.Code == code && b.IsActive, cancellationToken);
        }

        public async Task<List<Brand>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Brands
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Brand brand, CancellationToken cancellationToken = default)
        {
            await _context.Brands.AddAsync(brand, cancellationToken);
        }

        public async Task UpdateAsync(Brand brand, CancellationToken cancellationToken = default)
        {
            _context.Brands.Update(brand);
        }
    }
}
