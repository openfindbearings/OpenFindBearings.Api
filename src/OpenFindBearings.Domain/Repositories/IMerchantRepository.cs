using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Specifications;

namespace OpenFindBearings.Domain.Repositories
{
    public interface IMerchantRepository
    {
        Task<Merchant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Merchant>> SearchAsync(MerchantSearchParams searchParams, CancellationToken cancellationToken = default);
        Task AddAsync(Merchant merchant, CancellationToken cancellationToken = default);
        Task UpdateAsync(Merchant merchant, CancellationToken cancellationToken = default);
    }
}
