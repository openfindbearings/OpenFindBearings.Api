using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Domain.Interfaces
{
    /// <summary>
    /// 用户仓储接口
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// 根据认证用户ID获取用户
        /// </summary>
        Task<User?> GetByAuthUserIdAsync(string authUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据业务用户ID获取用户
        /// </summary>
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据游客会话ID获取用户
        /// </summary>
        Task<User?> GetByGuestSessionIdAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取商家的所有员工
        /// </summary>
        Task<IEnumerable<User>> GetByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有管理员
        /// </summary>
        Task<IEnumerable<User>> GetAdminsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 分页获取用户列表
        /// </summary>
        Task<PagedResult<User>> GetPagedAsync(
            string? keyword = null,
            UserType? userType = null,
            bool? isActive = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加用户
        /// </summary>
        Task AddAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新用户
        /// </summary>
        Task UpdateAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查用户是否存在
        /// </summary>
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
