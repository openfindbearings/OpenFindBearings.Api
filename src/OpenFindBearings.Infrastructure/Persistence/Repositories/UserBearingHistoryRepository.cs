using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 用户轴承浏览历史仓储实现
    /// </summary>
    public class UserBearingHistoryRepository : IUserBearingHistoryRepository
    {
        private readonly ApplicationDbContext _context;

        public UserBearingHistoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserBearingHistory?> GetAsync(Guid userId, Guid bearingId, CancellationToken cancellationToken = default)
        {
            return await _context.UserBearingHistories
                .FirstOrDefaultAsync(ubh => ubh.UserId == userId && ubh.BearingId == bearingId, cancellationToken);
        }

        public async Task<List<UserBearingHistory>> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            return await _context.UserBearingHistories
                .Include(ubh => ubh.Bearing)
                .Where(ubh => ubh.UserId == userId)
                .OrderByDescending(ubh => ubh.ViewedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Guid>> GetBearingIdsByUserIdAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default)
        {
            return await _context.UserBearingHistories
                .Where(ubh => ubh.UserId == userId)
                .OrderByDescending(ubh => ubh.ViewedAt)
                .Take(limit)
                .Select(ubh => ubh.BearingId)
                .ToListAsync(cancellationToken);
        }

        public async Task AddOrUpdateAsync(Guid userId, Guid bearingId, CancellationToken cancellationToken = default)
        {
            var existing = await GetAsync(userId, bearingId, cancellationToken);
            if (existing != null)
            {
                existing.UpdateViewTime();
                _context.UserBearingHistories.Update(existing);
            }
            else
            {
                var history = new UserBearingHistory(userId, bearingId);
                await _context.UserBearingHistories.AddAsync(history, cancellationToken);
            }
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task ClearByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var histories = await _context.UserBearingHistories
                .Where(ubh => ubh.UserId == userId)
                .ToListAsync(cancellationToken);

            if (histories.Any())
            {
                _context.UserBearingHistories.RemoveRange(histories);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var history = await _context.UserBearingHistories.FindAsync(new object[] { id }, cancellationToken);
            if (history != null)
            {
                _context.UserBearingHistories.Remove(history);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserBearingHistories
                .CountAsync(ubh => ubh.UserId == userId, cancellationToken);
        }
    }
}
