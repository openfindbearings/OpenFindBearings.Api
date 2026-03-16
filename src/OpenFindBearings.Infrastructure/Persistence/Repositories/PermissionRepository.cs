using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 权限仓储实现
    /// </summary>
    public class PermissionRepository : IPermissionRepository
    {
        private readonly AppDbContext _context;

        public PermissionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Permissions
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken);
        }

        public async Task<Permission?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _context.Permissions
                .FirstOrDefaultAsync(p => p.Name == name && p.IsActive, cancellationToken);
        }

        public async Task<List<Permission>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Permissions
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Permission>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
        {
            return await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission)
                .Where(p => p.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Permission>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // 通过用户的角色获取所有权限（去重）
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission)
                .Where(p => p.IsActive)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Permission permission, CancellationToken cancellationToken = default)
        {
            await _context.Permissions.AddAsync(permission, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task AddRangeAsync(IEnumerable<Permission> permissions, CancellationToken cancellationToken = default)
        {
            await _context.Permissions.AddRangeAsync(permissions, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Permission permission, CancellationToken cancellationToken = default)
        {
            _context.Permissions.Update(permission);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var permission = await GetByIdAsync(id, cancellationToken);
            if (permission != null)
            {
                permission.Deactivate();
                await UpdateAsync(permission, cancellationToken);
            }
        }
    }
}
