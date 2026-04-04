using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    /// <summary>
    /// 用户收藏轴承仓储接口
    /// 对应接口：POST /api/favorites/bearings/{bearingId}、GET /api/favorites/bearings
    /// </summary>
    public interface IUserBearingFavoriteRepository
    {
        /// <summary>
        /// 根据用户ID和轴承ID获取收藏
        /// </summary>
        Task<UserBearingFavorite?> GetAsync(Guid userId, Guid bearingId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户的收藏列表
        /// </summary>
        Task<List<UserBearingFavorite>> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户的收藏轴承ID列表
        /// </summary>
        Task<List<Guid>> GetBearingIdsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查用户是否已收藏轴承
        /// </summary>
        Task<bool> ExistsAsync(Guid userId, Guid bearingId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取收藏数量
        /// </summary>
        Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加收藏
        /// </summary>
        Task AddAsync(UserBearingFavorite favorite, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除收藏
        /// </summary>
        Task DeleteAsync(Guid userId, Guid bearingId, CancellationToken cancellationToken = default);
    }
}
