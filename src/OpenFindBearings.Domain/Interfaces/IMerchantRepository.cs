using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Specifications;

namespace OpenFindBearings.Domain.Interfaces
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
        Task<IEnumerable<Merchant>> SearchAsync(MerchantSearchParams searchParams, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加商家
        /// </summary>
        Task AddAsync(Merchant merchant, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新商家
        /// </summary>
        Task UpdateAsync(Merchant merchant, CancellationToken cancellationToken = default);
    }
}
