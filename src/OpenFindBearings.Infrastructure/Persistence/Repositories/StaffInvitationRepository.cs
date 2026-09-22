using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
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
        /// 按主键获取邀请记录
        /// </summary>
        public async Task<StaffInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.StaffInvitations
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
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
        /// 按手机号或邮箱查询待确认员工邀请（v2.9.0 邀请确认制；任一联系方式命中即可，7 天有效期内）
        /// </summary>
        public async Task<List<StaffInvitation>> GetPendingStaffInvitationsByContactAsync(
            string? phone,
            string? email,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(email))
                return [];

            var now = DateTime.UtcNow;
            var query = _context.StaffInvitations
                .Where(i => i.Type == OpenFindBearings.Domain.Enums.InvitationType.Staff &&
                            i.Status == OpenFindBearings.Domain.Enums.InvitationStatus.Pending &&
                            i.CreatedAt.AddDays(7) > now);

            var p = phone ?? string.Empty;
            var e = email ?? string.Empty;
            query = query.Where(i => (!string.IsNullOrEmpty(p) && i.Phone == p) ||
                                     (!string.IsNullOrEmpty(e) && i.Email == e));

            return await query.OrderByDescending(i => i.CreatedAt).ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 按商户查询待确认员工邀请（未过期，按创建时间倒序）
        /// </summary>
        public async Task<List<StaffInvitation>> GetPendingStaffInvitationsByMerchantAsync(
            Guid merchantId,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _context.StaffInvitations
                .Where(i => i.MerchantId == merchantId &&
                            i.Type == OpenFindBearings.Domain.Enums.InvitationType.Staff &&
                            i.Status == OpenFindBearings.Domain.Enums.InvitationStatus.Pending &&
                            i.CreatedAt.AddDays(7) > now)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 批量查出被进行中提名锁定的商户ID（未过期 Nomination Pending/Accepted）
        /// </summary>
        public async Task<HashSet<Guid>> GetNominationLockedMerchantIdsAsync(
            IEnumerable<Guid> merchantIds,
            CancellationToken cancellationToken = default)
        {
            var ids = merchantIds.ToList();
            if (ids.Count == 0) return [];

            var now = DateTime.UtcNow;
            var locked = await _context.StaffInvitations
                .Where(i => ids.Contains(i.MerchantId) &&
                            i.Type == OpenFindBearings.Domain.Enums.InvitationType.Nomination &&
                            (i.Status == OpenFindBearings.Domain.Enums.InvitationStatus.Pending ||
                             i.Status == OpenFindBearings.Domain.Enums.InvitationStatus.Accepted) &&
                            i.CreatedAt.AddDays(7) >= now)
                .Select(i => i.MerchantId)
                .Distinct()
                .ToListAsync(cancellationToken);
            return locked.ToHashSet();
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

        /// <summary>商户全部待确认邀请（StaffJoin+Nomination 均含，接管作废用）</summary>
        public async Task<List<StaffInvitation>> GetPendingByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<StaffInvitation>()
                .Where(i => i.MerchantId == merchantId && i.Status == InvitationStatus.Pending)
                .ToListAsync(cancellationToken);
        }
    }
}
