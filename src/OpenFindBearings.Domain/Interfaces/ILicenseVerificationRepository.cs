using OpenFindBearings.Domain.Common.Models;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Interfaces
{
    public interface ILicenseVerificationRepository
    {
        /// <summary>
        /// 根据ID获取审核记录
        /// </summary>
        Task<LicenseVerification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取待审核列表
        /// </summary>
        Task<PagedResult<LicenseVerification>> GetPendingAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取商家的审核记录
        /// </summary>
        Task<List<LicenseVerification>> GetByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加审核记录
        /// </summary>
        Task AddAsync(LicenseVerification verification, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新审核记录
        /// </summary>
        Task UpdateAsync(LicenseVerification verification, CancellationToken cancellationToken = default);
    }
}
