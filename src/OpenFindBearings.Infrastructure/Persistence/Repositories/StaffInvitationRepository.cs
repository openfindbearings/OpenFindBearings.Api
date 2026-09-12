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
        /// 获取某商户指定类型的最新邀请
        /// </summary>
        public async Task<StaffInvitation?> GetLatestByMerchantAndTypeAsync(
            Guid merchantId,
            OpenFindBearings.Domain.Enums.InvitationType type,
            CancellationToken cancellationToken = default)
        {
            return await _context.StaffInvitations
                .Where(i => i.MerchantId == merchantId && i.Type == type)
                .OrderByDescending(i => i.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// 按手机号查询待接受的提名邀请（未过期，按创建时间倒序）
        /// </summary>
        public async Task<List<StaffInvitation>> GetPendingNominationsByPhoneAsync(
            string phone,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _context.StaffInvitations
                .Where(i => i.Phone == phone &&
                            i.Type == OpenFindBearings.Domain.Enums.InvitationType.Nomination &&
                            i.Status == OpenFindBearings.Domain.Enums.InvitationStatus.Pending &&
                            // 有效期与 IsExpired() 口径一致：创建后 7 天
                            i.CreatedAt.AddDays(7) > now)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 添加邀请记录
        /// </summary>
        public async Task AddAsync(StaffInvitation invitation, CancellationToken cancellationToken = default)
        {
            await _context.StaffInvitations.AddAsync(invitation, cancellationToken);
            
        }

        /// <summary>
        /// 更新邀请记录
        /// </summary>
        public async Task UpdateAsync(StaffInvitation invitation, CancellationToken cancellationToken = default)
        {
            _context.StaffInvitations.Update(invitation);
            
        }
    }
}
