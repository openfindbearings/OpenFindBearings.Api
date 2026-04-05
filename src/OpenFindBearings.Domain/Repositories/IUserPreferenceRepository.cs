using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    /// <summary>
    /// 用户偏好仓储接口
    /// </summary>
    public interface IUserPreferenceRepository
    {
        /// <summary>
        /// 根据用户ID获取偏好设置
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>用户偏好设置，不存在则返回null</returns>
        Task<UserPreference?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加用户偏好设置
        /// </summary>
        /// <param name="preference">用户偏好设置</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task AddAsync(UserPreference preference, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新用户偏好设置
        /// </summary>
        /// <param name="preference">用户偏好设置</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task UpdateAsync(UserPreference preference, CancellationToken cancellationToken = default);
    }
}
