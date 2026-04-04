using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 用户-角色关联仓储实现
    /// </summary>
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRoleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddUserToRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
        {
            // 检查是否已存在
            var exists = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

            if (!exists)
            {
                // 使用带参数的构造函数
                var userRole = new UserRole(userId, roleId);
                await _context.UserRoles.AddAsync(userRole, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task AddUserToRolesAsync(Guid userId, IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default)
        {
            foreach (var roleId in roleIds)
            {
                await AddUserToRoleAsync(userId, roleId, cancellationToken);
            }
        }

        public async Task RemoveUserFromRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

            if (userRole != null)
            {
                _context.UserRoles.Remove(userRole);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task RemoveUserFromAllRolesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .ToListAsync(cancellationToken);

            if (userRoles.Any())
            {
                _context.UserRoles.RemoveRange(userRoles);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<bool> UserHasRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == roleName, cancellationToken);
        }

        public async Task<bool> UserHasRoleIdAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
        {
            return await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
        }
    }
}
