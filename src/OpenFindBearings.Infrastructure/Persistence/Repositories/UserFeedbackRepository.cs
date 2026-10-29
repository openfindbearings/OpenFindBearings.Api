using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;
using OpenFindBearings.Infrastructure.Persistence.Extensions;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 意见反馈仓储实现
    /// 管理端列表在仓储内与用户表左连接，一次带出提交人昵称与脱敏手机号
    /// </summary>
    public class UserFeedbackRepository : IUserFeedbackRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        public UserFeedbackRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 新增反馈（仅加入变更跟踪，提交由 UnitOfWork 负责）
        /// </summary>
        public async Task AddAsync(UserFeedback feedback, CancellationToken cancellationToken = default)
        {
            await _context.UserFeedbacks.AddAsync(feedback, cancellationToken);
        }

        /// <summary>
        /// 按 ID 获取反馈
        /// </summary>
        public async Task<UserFeedback?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.UserFeedbacks.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        }

        /// <summary>
        /// 更新反馈（仅标记为已修改，提交由 UnitOfWork 负责）
        /// </summary>
        public Task UpdateAsync(UserFeedback feedback, CancellationToken cancellationToken = default)
        {
            _context.MarkModified(feedback);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 统计指定用户在给定时间点之后提交的反馈条数（用于每日限频）
        /// </summary>
        public async Task<int> CountSinceAsync(Guid userId, DateTime sinceUtc, CancellationToken cancellationToken = default)
        {
            return await _context.UserFeedbacks
                .CountAsync(f => f.UserId == userId && f.CreatedAt >= sinceUtc, cancellationToken);
        }

        /// <summary>
        /// 分页查询指定用户自己的反馈（按提交时间倒序）
        /// </summary>
        public async Task<PagedResult<UserFeedback>> SearchByUserAsync(
            Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var (safePage, safePageSize) = NormalizePaging(page, pageSize);

            var query = _context.UserFeedbacks.AsNoTracking().Where(f => f.UserId == userId);
            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<UserFeedback>(items, totalCount, safePage, safePageSize);
        }

        /// <summary>
        /// 管理端分页查询反馈
        /// 改动说明（v2.22.1 意见反馈）：与用户表左连接取昵称与手机号，
        /// 手机号在仓储层脱敏（只露前 3 位与后 4 位），避免仅有 feedback.view 权限的角色看到完整号码
        /// </summary>
        public async Task<PagedResult<FeedbackListItem>> SearchAsync(
            FeedbackSearchParams searchParams, CancellationToken cancellationToken = default)
        {
            var (page, pageSize) = NormalizePaging(searchParams.Page, searchParams.PageSize);

            var query = _context.UserFeedbacks.AsNoTracking();

            if (searchParams.Status.HasValue)
                query = query.Where(f => f.Status == searchParams.Status.Value);

            if (searchParams.Category.HasValue)
                query = query.Where(f => f.Category == searchParams.Category.Value);

            if (!string.IsNullOrWhiteSpace(searchParams.Keyword))
            {
                var keyword = searchParams.Keyword.Trim();
                query = query.Where(f => f.Content.Contains(keyword)
                    || (f.Contact != null && f.Contact.Contains(keyword)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var rows = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new
                {
                    Feedback = f,
                    UserNickname = _context.Users
                        .Where(u => u.Id == f.UserId)
                        .Select(u => u.Nickname)
                        .FirstOrDefault(),
                    UserMobile = _context.Users
                        .Where(u => u.Id == f.UserId)
                        .Select(u => u.Mobile)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            var items = rows.Select(r => new FeedbackListItem
            {
                Id = r.Feedback.Id,
                UserId = r.Feedback.UserId,
                UserNickname = r.UserNickname,
                UserMobile = MaskMobile(r.UserMobile),
                Category = r.Feedback.Category,
                Content = r.Feedback.Content,
                ImageUrls = r.Feedback.ImageUrls,
                Contact = r.Feedback.Contact,
                ClientInfo = r.Feedback.ClientInfo,
                Status = r.Feedback.Status,
                AdminReply = r.Feedback.AdminReply,
                RepliedAt = r.Feedback.RepliedAt,
                CreatedAt = r.Feedback.CreatedAt
            }).ToList();

            return new PagedResult<FeedbackListItem>(items, totalCount, page, pageSize);
        }

        /// <summary>
        /// 分页参数归一化：页码最小 1，每页 1 到 100 条
        /// </summary>
        private static (int Page, int PageSize) NormalizePaging(int page, int pageSize)
        {
            var safePage = page < 1 ? 1 : page;
            var safePageSize = pageSize switch
            {
                < 1 => 20,
                > 100 => 100,
                _ => pageSize
            };
            return (safePage, safePageSize);
        }

        /// <summary>
        /// 手机号脱敏：保留前 3 位与后 4 位，长度不足时原样返回
        /// </summary>
        private static string? MaskMobile(string? mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile))
                return null;

            var value = mobile.Trim();
            if (value.Length < 8)
                return value;

            return $"{value[..3]}****{value[^4..]}";
        }
    }
}