using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Interfaces
{
    /// <summary>
    /// 角色-权限关联仓储接口
    /// 负责角色和权限之间关联关系的操作
    /// </summary>
    public interface IRolePermissionRepository
    {
        /// <summary>
        /// 为角色添加权限
        /// </summary>
        Task AddPermissionToRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量添加权限到角色
        /// </summary>
        Task AddPermissionsToRoleAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// 移除角色的权限
        /// </summary>
        Task RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 清空角色的所有权限
        /// </summary>
        Task ClearRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查角色是否有指定权限
        /// </summary>
        Task<bool> RoleHasPermissionAsync(Guid roleId, string permissionName, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查角色是否有指定权限ID
        /// </summary>
        Task<bool> RoleHasPermissionIdAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取角色的权限列表
        /// </summary>
        Task<List<Permission>> GetPermissionsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    }
}
