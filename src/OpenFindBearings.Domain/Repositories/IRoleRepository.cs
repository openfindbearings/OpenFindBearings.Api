using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    /// <summary>
    /// 角色仓储接口
    /// 负责角色的持久化操作
    /// </summary>
    public interface IRoleRepository
    {
        /// <summary>
        /// 根据ID获取角色
        /// </summary>
        Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据名称获取角色
        /// </summary>
        Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有角色
        /// </summary>
        Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户的角色列表
        /// </summary>
        Task<List<Role>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加角色
        /// </summary>
        Task AddAsync(Role role, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新角色
        /// </summary>
        Task UpdateAsync(Role role, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除角色（软删除）
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查角色是否存在
        /// </summary>
        Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
    }
}
