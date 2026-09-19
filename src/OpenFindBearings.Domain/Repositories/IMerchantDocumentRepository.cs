using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    /// <summary>
    /// 商户证照材料仓储接口（v2.7.0 由 ILicenseVerificationRepository 泛化：执照/授权书/厂房照）
    /// </summary>
    public interface IMerchantDocumentRepository
    {
        /// <summary>
        /// 根据ID获取审核记录
        /// </summary>
        Task<MerchantDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取待审核列表
        /// </summary>
        Task<PagedResult<MerchantDocument>> GetPendingAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取商家的审核记录
        /// </summary>
        Task<List<MerchantDocument>> GetByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加审核记录
        /// </summary>
        Task AddAsync(MerchantDocument document, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新审核记录
        /// </summary>
        Task UpdateAsync(MerchantDocument document, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取待审核记录数量
        /// </summary>
        Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
    }
}
