using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByAuthUserIdAsync(string authUserId, CancellationToken cancellationToken = default);
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(User user, CancellationToken cancellationToken = default);
        Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    }
}
