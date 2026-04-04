using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Specifications;

namespace OpenFindBearings.Domain.Repositories
{
    /// <summary>
    /// 商家仓储接口
    /// 负责商家实体的持久化操作
    /// </summary>
    public interface IMerchantRepository
    {
        /// <summary>
        /// 根据ID获取商家
        /// </summary>
        Task<Merchant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据用户ID获取商家（通过员工关联）
        /// </summary>
        Task<Merchant?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 搜索商家
        /// </summary>
        Task<PagedResult<Merchant>> SearchAsync(MerchantSearchParams searchParams, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加商家
        /// </summary>
        Task AddAsync(Merchant merchant, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新商家
        /// </summary>
        Task UpdateAsync(Merchant merchant, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查商家名称是否存在
        /// </summary>
        Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取商家总数
        /// </summary>
        Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    }
}
