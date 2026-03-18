using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 用户商家浏览历史仓储实现
    /// </summary>
    public class UserMerchantHistoryRepository : IUserMerchantHistoryRepository
    {
        private readonly AppDbContext _context;

        public UserMerchantHistoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UserMerchantHistory?> GetAsync(Guid userId, Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.UserMerchantHistories
                .FirstOrDefaultAsync(umh => umh.UserId == userId && umh.MerchantId == merchantId, cancellationToken);
        }

        public async Task<List<UserMerchantHistory>> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            return await _context.UserMerchantHistories
                .Include(umh => umh.Merchant)
                .Where(umh => umh.UserId == userId)
                .OrderByDescending(umh => umh.ViewedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Guid>> GetMerchantIdsByUserIdAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default)
        {
            return await _context.UserMerchantHistories
                .Where(umh => umh.UserId == userId)
                .OrderByDescending(umh => umh.ViewedAt)
                .Take(limit)
                .Select(umh => umh.MerchantId)
                .ToListAsync(cancellationToken);
        }

        public async Task AddOrUpdateAsync(Guid userId, Guid merchantId, CancellationToken cancellationToken = default)
        {
            var existing = await GetAsync(userId, merchantId, cancellationToken);
            if (existing != null)
            {
                existing.UpdateViewTime();
                _context.UserMerchantHistories.Update(existing);
            }
            else
            {
                var history = new UserMerchantHistory(userId, merchantId);
                await _context.UserMerchantHistories.AddAsync(history, cancellationToken);
            }
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task ClearByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var histories = await _context.UserMerchantHistories
                .Where(umh => umh.UserId == userId)
                .ToListAsync(cancellationToken);

            if (histories.Any())
            {
                _context.UserMerchantHistories.RemoveRange(histories);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var history = await _context.UserMerchantHistories.FindAsync(new object[] { id }, cancellationToken);
            if (history != null)
            {
                _context.UserMerchantHistories.Remove(history);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
