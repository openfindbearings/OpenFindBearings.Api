using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 角色仓储实现
    /// </summary>
    public class RoleRepository : IRoleRepository
    {
        private readonly AppDbContext _context;

        public RoleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);
        }

        public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Name == name && r.IsActive, cancellationToken);
        }

        public async Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .Where(r => r.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Role>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role)
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .Where(r => r.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
        {
            await _context.Roles.AddAsync(role, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
        {
            _context.Roles.Update(role);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var role = await GetByIdAsync(id, cancellationToken);
            if (role != null)
            {
                role.Deactivate();
                await UpdateAsync(role, cancellationToken);
            }
        }

        public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _context.Roles
                .AnyAsync(r => r.Name == name && r.IsActive, cancellationToken);
        }
    }
}
