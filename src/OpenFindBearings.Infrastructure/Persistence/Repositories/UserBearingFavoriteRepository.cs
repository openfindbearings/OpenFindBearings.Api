using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 用户收藏轴承仓储实现
    /// </summary>
    public class UserBearingFavoriteRepository : IUserBearingFavoriteRepository
    {
        private readonly ApplicationDbContext _context;

        public UserBearingFavoriteRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserBearingFavorite?> GetAsync(Guid userId, Guid bearingId, CancellationToken cancellationToken = default)
        {
            return await _context.UserFavorites
                .FirstOrDefaultAsync(uf => uf.UserId == userId && uf.BearingId == bearingId, cancellationToken);
        }

        public async Task<List<UserBearingFavorite>> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            return await _context.UserFavorites
                .Include(uf => uf.Bearing)
                    .ThenInclude(b => b.Brand)
                .Where(uf => uf.UserId == userId)
                .OrderByDescending(uf => uf.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Guid>> GetBearingIdsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserFavorites
                .Where(uf => uf.UserId == userId)
                .Select(uf => uf.BearingId)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(Guid userId, Guid bearingId, CancellationToken cancellationToken = default)
        {
            return await _context.UserFavorites
                .AnyAsync(uf => uf.UserId == userId && uf.BearingId == bearingId, cancellationToken);
        }

        public async Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserFavorites
                .CountAsync(uf => uf.UserId == userId, cancellationToken);
        }

        public async Task AddAsync(UserBearingFavorite favorite, CancellationToken cancellationToken = default)
        {
            await _context.UserFavorites.AddAsync(favorite, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid userId, Guid bearingId, CancellationToken cancellationToken = default)
        {
            var favorite = await GetAsync(userId, bearingId, cancellationToken);
            if (favorite != null)
            {
                _context.UserFavorites.Remove(favorite);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
