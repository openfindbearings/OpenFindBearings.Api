using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    public interface IStaffInvitationRepository
    {
        Task<StaffInvitation?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task AddAsync(StaffInvitation invitation, CancellationToken cancellationToken = default);
        Task UpdateAsync(StaffInvitation invitation, CancellationToken cancellationToken = default);
    }
}
