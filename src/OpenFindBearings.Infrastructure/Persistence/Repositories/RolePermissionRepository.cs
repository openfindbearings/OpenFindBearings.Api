using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 角色-权限关联仓储实现
    /// </summary>
  public class RolePermissionRepository : IRolePermissionRepository
    {
        private readonly AppDbContext _context;

        public RolePermissionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddPermissionToRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
        {
            // 检查是否已存在
            var exists = await _context.RolePermissions
                .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);

            if (!exists)
            {
                // 使用带参数的构造函数
                var rolePermission = new RolePermission(roleId, permissionId);
                await _context.RolePermissions.AddAsync(rolePermission, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task AddPermissionsToRoleAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default)
        {
            foreach (var permissionId in permissionIds)
            {
                await AddPermissionToRoleAsync(roleId, permissionId, cancellationToken);
            }
        }

        public async Task RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
        {
            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);

            if (rolePermission != null)
            {
                _context.RolePermissions.Remove(rolePermission);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task ClearRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
        {
            var rolePermissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync(cancellationToken);

            if (rolePermissions.Any())
            {
                _context.RolePermissions.RemoveRange(rolePermissions);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<bool> RoleHasPermissionAsync(Guid roleId, string permissionName, CancellationToken cancellationToken = default)
        {
            return await _context.RolePermissions
                .Include(rp => rp.Permission)
                .AnyAsync(rp => rp.RoleId == roleId && rp.Permission.Name == permissionName, cancellationToken);
        }

        public async Task<bool> RoleHasPermissionIdAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
        {
            return await _context.RolePermissions
                .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);
        }

        public async Task<List<Permission>> GetPermissionsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
        {
            return await _context.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission)
                .ToListAsync(cancellationToken);
        }
    }
}
