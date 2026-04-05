using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Domain.Specifications;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 用户仓储实现
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<User?> GetByAuthUserIdAsync(string authUserId, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.Merchant)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.AuthUserId == authUserId && u.IsActive, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.Merchant)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<User>> GetByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.MerchantId == merchantId && u.IsActive)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<User>> GetAdminsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Admin") && u.IsActive)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await _context.Users.AddAsync(user, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<User?> GetByGuestSessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.GuestSessionId == sessionId && u.IsGuest, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<PagedResult<User>> SearchAsync(
            SearchUserParams searchParams,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Users.AsQueryable();

            // 关键词搜索（昵称或AuthUserId）
            if (!string.IsNullOrEmpty(searchParams.Keyword))
            {
                query = query.Where(u =>
                    (u.Nickname != null && u.Nickname.Contains(searchParams.Keyword)) ||
                    u.AuthUserId.Contains(searchParams.Keyword));
            }

            // 角色筛选
            if (!string.IsNullOrEmpty(searchParams.RoleName))
            {
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == searchParams.RoleName));
            }

            // 活跃状态筛选
            if (searchParams.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == searchParams.IsActive.Value);
            }

            // 是否已合并筛选
            if (searchParams.IsMerged.HasValue)
            {
                query = query.Where(u => u.IsMerged == searchParams.IsMerged.Value);
            }

            // 是否是游客
            if (searchParams.IsGuest.HasValue)
            {
                query = query.Where(u => u.IsGuest == searchParams.IsGuest.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            // 排序
            query = searchParams.SortBy?.ToLower() switch
            {
                "createdat" => searchParams.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(u => u.CreatedAt)
                    : query.OrderBy(u => u.CreatedAt),
                "lastloginat" => searchParams.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(u => u.LastLoginAt)
                    : query.OrderBy(u => u.LastLoginAt),
                "nickname" => searchParams.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(u => u.Nickname)
                    : query.OrderBy(u => u.Nickname),
                _ => query.OrderByDescending(u => u.CreatedAt)
            };

            var items = await query
                .Include(u => u.Merchant)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Skip((searchParams.Page - 1) * searchParams.PageSize)
                .Take(searchParams.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<User>
            {
                Items = items,
                TotalCount = totalCount,
                Page = searchParams.Page,
                PageSize = searchParams.PageSize
            };
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Users.AnyAsync(u => u.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateSearchStatsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await GetByIdAsync(userId, cancellationToken);
            if (user != null)
            {
                user.RecordSearch();
                await UpdateAsync(user, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public async Task UpdateQueryStatsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await GetByIdAsync(userId, cancellationToken);
            if (user != null)
            {
                user.RecordQuery();
                await UpdateAsync(user, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public async Task UpdateLastActiveAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await GetByIdAsync(userId, cancellationToken);
            if (user != null)
            {
                user.UpdateLastActive();
                await UpdateAsync(user, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<User>> GetByLevelAsync(UserLevel level, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Where(u => u.Level == level && u.IsActive)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<User>> GetExpiringSubscriptionsAsync(DateTime threshold, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Where(u => u.Level == UserLevel.Premium &&
                       u.SubscriptionExpiry != null &&
                       u.SubscriptionExpiry <= threshold &&
                       u.IsActive)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var user = await GetByIdAsync(id, cancellationToken);
            if (user != null)
            {
                user.Disable();
                await UpdateAsync(user, cancellationToken);
            }
        }
    }
}
