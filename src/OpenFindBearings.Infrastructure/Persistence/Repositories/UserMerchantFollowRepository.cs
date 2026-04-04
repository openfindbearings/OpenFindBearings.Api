using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 用户关注商家仓储实现
    /// </summary>
    public class UserMerchantFollowRepository : IUserMerchantFollowRepository
    {
        private readonly ApplicationDbContext _context;

        public UserMerchantFollowRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserMerchantFollow?> GetAsync(Guid userId, Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.UserFollows
                .FirstOrDefaultAsync(uf => uf.UserId == userId && uf.MerchantId == merchantId, cancellationToken);
        }

        public async Task<List<UserMerchantFollow>> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            return await _context.UserFollows
                .Include(uf => uf.Merchant)
                .Where(uf => uf.UserId == userId)
                .OrderByDescending(uf => uf.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Guid>> GetMerchantIdsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserFollows
                .Where(uf => uf.UserId == userId)
                .Select(uf => uf.MerchantId)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(Guid userId, Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.UserFollows
                .AnyAsync(uf => uf.UserId == userId && uf.MerchantId == merchantId, cancellationToken);
        }

        public async Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserFollows
                .CountAsync(uf => uf.UserId == userId, cancellationToken);
        }

        public async Task<int> CountFollowersByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.UserFollows
                .CountAsync(uf => uf.MerchantId == merchantId, cancellationToken);
        }

        public async Task AddAsync(UserMerchantFollow follow, CancellationToken cancellationToken = default)
        {
            await _context.UserFollows.AddAsync(follow, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid userId, Guid merchantId, CancellationToken cancellationToken = default)
        {
            var follow = await GetAsync(userId, merchantId, cancellationToken);
            if (follow != null)
            {
                _context.UserFollows.Remove(follow);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
