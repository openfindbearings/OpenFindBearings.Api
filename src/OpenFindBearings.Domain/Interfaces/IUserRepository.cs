using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Interfaces
{
    /// <summary>
    /// 用户仓储接口
    /// 负责用户实体的持久化操作
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// 根据认证用户ID获取用户
        /// </summary>
        Task<User?> GetByAuthUserIdAsync(string authUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据ID获取用户
        /// </summary>
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据邮箱获取用户
        /// </summary>
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取商家的所有员工
        /// </summary>
        Task<IEnumerable<User>> GetByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加用户
        /// </summary>
        Task AddAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新用户
        /// </summary>
        Task UpdateAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有管理员
        /// </summary>
        Task<IEnumerable<User>> GetAdminsAsync(CancellationToken cancellationToken = default);
    }
}
