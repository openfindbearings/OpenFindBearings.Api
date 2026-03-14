using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    public interface IMerchantBearingRepository
    {
        Task<MerchantBearing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<MerchantBearing>> GetByBearingAsync(Guid bearingId, CancellationToken cancellationToken = default);
        Task<IEnumerable<MerchantBearing>> GetByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default);
        Task AddAsync(MerchantBearing merchantBearing, CancellationToken cancellationToken = default);
        Task UpdateAsync(MerchantBearing merchantBearing, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }

}
