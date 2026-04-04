using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 员工邀请仓储实现
    /// </summary>
    public class StaffInvitationRepository : IStaffInvitationRepository
    {
        private readonly ApplicationDbContext _context;

        public StaffInvitationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 根据邀请码获取邀请记录
        /// </summary>
        public async Task<StaffInvitation?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _context.StaffInvitations
                .FirstOrDefaultAsync(i => i.InvitationCode == code, cancellationToken);
        }

        /// <summary>
        /// 添加邀请记录
        /// </summary>
        public async Task AddAsync(StaffInvitation invitation, CancellationToken cancellationToken = default)
        {
            await _context.StaffInvitations.AddAsync(invitation, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// 更新邀请记录
        /// </summary>
        public async Task UpdateAsync(StaffInvitation invitation, CancellationToken cancellationToken = default)
        {
            _context.StaffInvitations.Update(invitation);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
