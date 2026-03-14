using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    public interface ICorrectionRequestRepository
    {
        Task<CorrectionRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<CorrectionRequest>> GetPendingAsync(string? entityType = null, CancellationToken cancellationToken = default);
        Task<List<CorrectionRequest>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task AddAsync(CorrectionRequest correction, CancellationToken cancellationToken = default);
        Task UpdateAsync(CorrectionRequest correction, CancellationToken cancellationToken = default);
    }
}
