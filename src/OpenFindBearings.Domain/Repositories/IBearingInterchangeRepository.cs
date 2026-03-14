using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    public interface IBearingInterchangeRepository
    {
        Task<BearingInterchange?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<BearingInterchange>> GetBySourceBearingAsync(Guid bearingId, CancellationToken cancellationToken = default);
        Task<List<BearingInterchange>> GetByTargetBearingAsync(Guid bearingId, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken = default);
        Task AddAsync(BearingInterchange interchange, CancellationToken cancellationToken = default);
        Task AddRangeAsync(List<BearingInterchange> interchanges, CancellationToken cancellationToken = default);
        Task UpdateAsync(BearingInterchange interchange, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
