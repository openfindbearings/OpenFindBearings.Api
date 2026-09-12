using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 商户成员仓储实现
    /// </summary>
    public class MerchantMemberRepository : IMerchantMemberRepository
    {
        private readonly ApplicationDbContext _context;

        public MerchantMemberRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<MerchantMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Set<MerchantMember>()
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }

        public async Task<MerchantMember?> GetActiveByUserAndMerchantAsync(Guid userId, Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<MerchantMember>()
                .FirstOrDefaultAsync(m =>
                    m.UserId == userId &&
                    m.MerchantId == merchantId &&
                    m.Status == MerchantMemberStatus.Active,
                    cancellationToken);
        }

        public async Task<MerchantMember?> GetByUserAndMerchantAsync(Guid userId, Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<MerchantMember>()
                .FirstOrDefaultAsync(m =>
                    m.UserId == userId &&
                    m.MerchantId == merchantId,
                    cancellationToken);
        }

        public async Task<List<MerchantMember>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<MerchantMember>()
                .Where(m => m.UserId == userId && m.Status == MerchantMemberStatus.Active)
                .OrderBy(m => m.JoinedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<MerchantMember>> GetActiveByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<MerchantMember>()
                .Include(m => m.User)
                .Where(m => m.MerchantId == merchantId && m.Status == MerchantMemberStatus.Active)
                .OrderBy(m => m.JoinedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountActiveAdminsAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<MerchantMember>()
                .CountAsync(m =>
                    m.MerchantId == merchantId &&
                    m.Status == MerchantMemberStatus.Active &&
                    m.Role == MerchantMember.RoleMerchantAdmin,
                    cancellationToken);
        }

        public async Task<int> CountActiveAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Set<MerchantMember>()
                .CountAsync(m => m.Status == MerchantMemberStatus.Active, cancellationToken);
        }

        public async Task AddAsync(MerchantMember member, CancellationToken cancellationToken = default)
        {
            await _context.Set<MerchantMember>().AddAsync(member, cancellationToken);
        }

        public async Task UpdateAsync(MerchantMember member, CancellationToken cancellationToken = default)
        {
            _context.Set<MerchantMember>().Update(member);
        }
    }
}
