using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    public class StaffInvitationRepository : IStaffInvitationRepository
    {
        private readonly AppDbContext _context;

        public StaffInvitationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<StaffInvitation?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _context.Set<StaffInvitation>()
                .FirstOrDefaultAsync(i => i.InvitationCode == code, cancellationToken);
        }

        public async Task AddAsync(StaffInvitation invitation, CancellationToken cancellationToken = default)
        {
            await _context.Set<StaffInvitation>().AddAsync(invitation, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(StaffInvitation invitation, CancellationToken cancellationToken = default)
        {
            _context.Set<StaffInvitation>().Update(invitation);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
