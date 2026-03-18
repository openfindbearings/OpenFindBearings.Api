using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Interfaces
{
    /// <summary>
    /// 用户关注商家仓储接口
    /// 对应接口：POST /api/favorites/merchants/{merchantId}、GET /api/favorites/merchants
    /// </summary>
    public interface IUserFollowRepository
    {
        /// <summary>
        /// 根据用户ID和商家ID获取关注
        /// </summary>
        Task<UserFollow?> GetAsync(Guid userId, Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户的关注列表
        /// </summary>
        Task<List<UserFollow>> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户的关注商家ID列表
        /// </summary>
        Task<List<Guid>> GetMerchantIdsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查用户是否已关注商家
        /// </summary>
        Task<bool> ExistsAsync(Guid userId, Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取关注数量
        /// </summary>
        Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取商家的粉丝数量
        /// </summary>
        Task<int> CountFollowersByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加关注
        /// </summary>
        Task AddAsync(UserFollow follow, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除关注
        /// </summary>
        Task DeleteAsync(Guid userId, Guid merchantId, CancellationToken cancellationToken = default);
    }
}
