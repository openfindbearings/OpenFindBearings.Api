using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Interfaces
{
    /// <summary>
    /// 用户轴承浏览历史仓储接口
    /// 对应接口：POST /api/history/bearings/{bearingId}、GET /api/history/bearings
    /// </summary>
    public interface IUserBearingHistoryRepository
    {
        /// <summary>
        /// 根据用户ID和轴承ID获取历史记录
        /// </summary>
        Task<UserBearingHistory?> GetAsync(Guid userId, Guid bearingId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户的轴承浏览历史
        /// </summary>
        Task<List<UserBearingHistory>> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户浏览过的轴承ID列表
        /// </summary>
        Task<List<Guid>> GetBearingIdsByUserIdAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加或更新浏览历史
        /// </summary>
        Task AddOrUpdateAsync(Guid userId, Guid bearingId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 清空用户的轴承浏览历史
        /// </summary>
        Task ClearByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除指定历史记录
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户浏览历史总数
        /// </summary>
        Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
