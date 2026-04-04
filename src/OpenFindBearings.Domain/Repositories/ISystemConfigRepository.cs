using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    public interface ISystemConfigRepository
    {
        Task<SystemConfig?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
        Task<List<SystemConfig>> GetAllAsync(CancellationToken cancellationToken = default);
        Task UpdateAsync(SystemConfig config, CancellationToken cancellationToken = default);
    }
}
