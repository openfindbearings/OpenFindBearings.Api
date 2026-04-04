using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
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

        public async Task<User?> GetByAuthUserIdAsync(string authUserId, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.Merchant)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.AuthUserId == authUserId && u.IsActive, cancellationToken);
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.Merchant)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive, cancellationToken);
        }

        /// <summary>
        /// 获取商家的所有员工
        /// </summary>
        public async Task<IEnumerable<User>> GetByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.MerchantId == merchantId && u.IsActive)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 获取所有管理员
        /// </summary>
        public async Task<IEnumerable<User>> GetAdminsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.UserType == UserType.Admin && u.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await _context.Users.AddAsync(user, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// 根据游客会话ID获取用户
        /// </summary>
        public async Task<User?> GetByGuestSessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.GuestSessionId == sessionId && u.UserType == UserType.Guest, cancellationToken);
        }

        /// <summary>
        /// 分页获取用户列表
        /// </summary>
        public async Task<PagedResult<User>> GetPagedAsync(
            string? keyword = null,
            UserType? userType = null,
            bool? isActive = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Users.AsQueryable();

            // 关键词搜索（昵称或AuthUserId）
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(u =>
                    (u.Nickname != null && u.Nickname.Contains(keyword)) ||
                    u.AuthUserId.Contains(keyword));
            }

            // 用户类型筛选
            if (userType.HasValue)
            {
                query = query.Where(u => u.UserType == userType.Value);
            }

            // 活跃状态筛选
            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Include(u => u.Merchant)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<User>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// 检查用户是否存在
        /// </summary>
        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Users.AnyAsync(u => u.Id == id, cancellationToken);
        }

        // ============ ✅ 新增方法 ============

        /// <summary>
        /// 更新用户搜索统计
        /// </summary>
        public async Task UpdateSearchStatsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await GetByIdAsync(userId, cancellationToken);
            if (user != null)
            {
                user.RecordSearch();
                await UpdateAsync(user, cancellationToken);
            }
        }

        /// <summary>
        /// 更新用户查询统计
        /// </summary>
        public async Task UpdateQueryStatsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await GetByIdAsync(userId, cancellationToken);
            if (user != null)
            {
                user.RecordQuery();
                await UpdateAsync(user, cancellationToken);
            }
        }

        /// <summary>
        /// 更新最后活跃时间
        /// </summary>
        public async Task UpdateLastActiveAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await GetByIdAsync(userId, cancellationToken);
            if (user != null)
            {
                user.UpdateLastActive();
                await UpdateAsync(user, cancellationToken);
            }
        }

        /// <summary>
        /// 根据等级获取用户
        /// </summary>
        public async Task<IEnumerable<User>> GetByLevelAsync(UserLevel level, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Where(u => u.Level == level && u.IsActive)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 获取即将过期的付费用户
        /// </summary>
        public async Task<IEnumerable<User>> GetExpiringSubscriptionsAsync(DateTime threshold, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Where(u => u.Level == UserLevel.Premium &&
                       u.SubscriptionExpiry != null &&
                       u.SubscriptionExpiry <= threshold &&
                       u.IsActive)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 软删除用户
        /// </summary>
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
