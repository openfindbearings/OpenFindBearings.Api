using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    /// <summary>
    /// 用户商家浏览历史仓储接口
    /// 对应接口：POST /api/history/merchants/{merchantId}、GET /api/history/merchants
    /// </summary>
    public interface IUserMerchantHistoryRepository
    {
        /// <summary>
        /// 根据用户ID和商家ID获取历史记录
        /// </summary>
        Task<UserMerchantHistory?> GetAsync(Guid userId, Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户的商家浏览历史
        /// </summary>
        Task<List<UserMerchantHistory>> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户浏览过的商家ID列表
        /// </summary>
        Task<List<Guid>> GetMerchantIdsByUserIdAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加或更新浏览历史
        /// </summary>
        Task AddOrUpdateAsync(Guid userId, Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 清空用户的商家浏览历史
        /// </summary>
        Task ClearByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除指定历史记录
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
