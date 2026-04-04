using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    /// <summary>
    /// 权限仓储接口
    /// 负责权限的持久化操作
    /// </summary>
    public interface IPermissionRepository
    {
        /// <summary>
        /// 根据ID获取权限
        /// </summary>
        Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据名称获取权限
        /// </summary>
        Task<Permission?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有权限
        /// </summary>
        Task<List<Permission>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取角色的权限列表
        /// </summary>
        Task<List<Permission>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户的权限列表（通过角色间接获取）
        /// </summary>
        Task<List<Permission>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加权限
        /// </summary>
        Task AddAsync(Permission permission, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量添加权限
        /// </summary>
        Task AddRangeAsync(IEnumerable<Permission> permissions, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新权限
        /// </summary>
        Task UpdateAsync(Permission permission, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除权限
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
