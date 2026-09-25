using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;
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
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.AuthUserId == authUserId && u.IsActive, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<User>> GetAdminsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Admin") && u.IsActive && u.DeactivatedAt == null)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await _context.Users.AddAsync(user, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            var entry = _context.Entry(user);
            if (entry.State != EntityState.Added)
            {
                entry.State = EntityState.Modified;
            }
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
            if (searchParams.Page < 1) searchParams.Page = 1;
            if (searchParams.PageSize < 1) searchParams.PageSize = 20;
            if (searchParams.PageSize > 100) searchParams.PageSize = 100;

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
    public async Task<int> GetCountSinceAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.IsActive && u.DeactivatedAt == null && u.CreatedAt >= since)
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<User>> GetDeactivatedBeforeAsync(DateTime cutoff, CancellationToken cancellationToken = default)
    {
        // 注销已满冷静期且未匿名化的用户（Job 每轮限量处理，防单轮事务过大）
        return await _context.Users
            .Where(u => u.DeactivatedAt != null && !u.IsAnonymized && u.DeactivatedAt <= cutoff)
            .OrderBy(u => u.DeactivatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AnonymizeCascadeAsync(User user, CancellationToken cancellationToken = default)
    {
        // 级联清除个人数据（个保法删除义务）：收藏/关注/浏览历史/偏好逐表 ExecuteDelete，
        //   行为日志（UserBehaviorLog）保留——匿名化后的统计聚合仍有效且不再关联自然人
        await _context.Set<UserBearingFavorite>().Where(x => x.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);
        await _context.Set<UserMerchantFollow>().Where(x => x.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);
        await _context.Set<UserBearingHistory>().Where(x => x.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);
        await _context.Set<UserMerchantHistory>().Where(x => x.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);
        await _context.Set<UserPreference>().Where(x => x.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);
        // v2.17.0：纠错历史属个人申请数据（含已审行），匿名化期一并删除（个保法删除义务）
        await _context.Set<CorrectionRequest>().Where(x => x.SubmittedBy == user.Id).ExecuteDeleteAsync(cancellationToken);

        // 改动说明（v1.34.0 注销清零补刀，审计 U2/U3/U4/U7 修复）：
        //   积分账户/流水兜底删（新版 T0 已清，此处覆盖旧版注销的存量用户）；
        //   通知兜底删（T0 后事件链可能给已注销用户新插孤儿行）；
        //   邀请表本人联系方式列擦除（受邀人行的 Phone/Email 快照属本人 PII，
        //     行保留作商户侧记录）；平台角色解绑（注销者不应再被算作管理员）
        await _context.Set<PointAccount>().Where(x => x.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);
        await _context.Set<PointTransaction>().Where(x => x.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);
        await _context.Set<Notification>().Where(x => x.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);
        // 邀请表联系方式擦除（受邀人快照列属本人 PII）：仅在号/邮箱非空时执行——
        //   防 null==null 匹配擦掉全部空值行；行保留作商户侧记录
        if (!string.IsNullOrWhiteSpace(user.Mobile))
        {
            var mobile = user.Mobile;
            await _context.Set<StaffInvitation>().Where(x => x.Phone == mobile)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Phone, (string?)null), cancellationToken);
        }

        user.MarkAnonymized();
        await _context.Set<User>().Where(u => u.Id == user.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.Nickname, user.Nickname)
                .SetProperty(u => u.Avatar, user.Avatar)
                .SetProperty(u => u.Mobile, user.Mobile)
                .SetProperty(u => u.Address, user.Address)
                .SetProperty(u => u.CompanyName, user.CompanyName)
                .SetProperty(u => u.Industry, user.Industry)
                .SetProperty(u => u.RegisterIp, user.RegisterIp)
                .SetProperty(u => u.GuestSessionId, user.GuestSessionId)
                // 改动说明（v1.34.0 审计 U11/U12）：职业画像属自报 PII 一并置空；
                //   游客合并指针置 null 消除匿名化用户与新数据的关联链
                .SetProperty(u => u.Occupation, (OpenFindBearings.Domain.Enums.UserOccupation?)null)
                .SetProperty(u => u.MergedToUserId, (Guid?)null)
                // 改动说明（v1.34.0 审计 U7）：IsActive 同步置 false——注销/匿名化用户
                //   不再被"活跃用户"统计口径计入（原恒 true 致 dashboard 虚高）
                .SetProperty(u => u.IsActive, false)
                .SetProperty(u => u.IsAnonymized, true)
                .SetProperty(u => u.UpdatedAt, user.UpdatedAt), cancellationToken);
    }

        /// <inheritdoc/>
        public async Task<Dictionary<string, int>> GetRoleDistributionAsync(CancellationToken cancellationToken = default)
        {
            return await _context.UserRoles
                .Where(ur => ur.User != null && ur.User.IsActive && ur.User.DeactivatedAt == null)
                .GroupBy(ur => ur.Role.Name)
                .Select(g => new { RoleName = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RoleName, x => x.Count, cancellationToken);
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
