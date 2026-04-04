using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    /// <summary>
    /// 商家轴承关联仓储接口
    /// 负责商家和轴承之间关联关系的操作
    /// </summary>
    public interface IMerchantBearingRepository
    {
        /// <summary>
        /// 根据ID获取关联
        /// </summary>
        Task<MerchantBearing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据轴承ID获取关联列表
        /// </summary>
        Task<IEnumerable<MerchantBearing>> GetByBearingAsync(Guid bearingId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据商家ID获取关联列表
        /// </summary>
        Task<IEnumerable<MerchantBearing>> GetByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取商家在售的轴承列表
        /// </summary>
        Task<IEnumerable<MerchantBearing>> GetOnSaleByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取待审核的关联列表
        /// </summary>
        Task<IEnumerable<MerchantBearing>> GetPendingApprovalAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查是否存在关联
        /// </summary>
        Task<bool> ExistsAsync(Guid merchantId, Guid bearingId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查轴承是否属于商家
        /// </summary>
        Task<bool> IsOwnedByMerchantAsync(Guid bearingId, Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加关联
        /// </summary>
        Task AddAsync(MerchantBearing merchantBearing, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量添加关联
        /// </summary>
        Task AddRangeAsync(IEnumerable<MerchantBearing> merchantBearings, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新关联
        /// </summary>
        Task UpdateAsync(MerchantBearing merchantBearing, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除关联
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
