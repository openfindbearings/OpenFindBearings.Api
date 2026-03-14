namespace OpenFindBearings.Domain.Repositories
{
    /// <summary>
    /// 用户-角色关联仓储接口
    /// 负责用户和角色之间关联关系的操作
    /// </summary>
    public interface IUserRoleRepository
    {
        /// <summary>
        /// 为用户添加角色
        /// </summary>
        Task AddUserToRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量为用户添加角色
        /// </summary>
        Task AddUserToRolesAsync(Guid userId, IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// 移除用户的角色
        /// </summary>
        Task RemoveUserFromRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 移除用户的所有角色
        /// </summary>
        Task RemoveUserFromAllRolesAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查用户是否有指定角色
        /// </summary>
        Task<bool> UserHasRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查用户是否有指定角色ID
        /// </summary>
        Task<bool> UserHasRoleIdAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    }
}
