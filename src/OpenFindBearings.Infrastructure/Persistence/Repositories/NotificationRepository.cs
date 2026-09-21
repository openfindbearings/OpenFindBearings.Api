using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 站内信仓储实现
    /// </summary>
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _context;

        public NotificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            await _context.Set<Notification>().AddAsync(notification, cancellationToken);
        }

        public Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            _context.Set<Notification>().Update(notification);
            return Task.CompletedTask;
        }

        public async Task<Notification?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Notification>()
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);
        }

        public async Task<(List<Notification> Items, int Total)> GetByUserAsync(Guid userId, bool unreadOnly,
            int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<Notification>().Where(n => n.UserId == userId);
            if (unreadOnly)
                query = query.Where(n => !n.IsRead);

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, total);
        }

        public async Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Notification>()
                .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
        }

        public async Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            // 改动说明：ExecuteUpdate 单条 SQL 批量置已读，避免先查后改的两次往返
            return await _context.Set<Notification>()
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, now)
                    .SetProperty(n => n.UpdatedAt, now), cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<int> MarkReadByTypeAsync(Guid userId, string type, Guid? bizId,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            // ExecuteUpdate 直改（与 MarkAllRead 同款）：本方法在命令处理事务内执行，
            //   随 UnitOfWork 管道统一提交，不走 AddInAppAsync 的独立提交语义
            return await _context.Set<Notification>()
                .Where(n => n.UserId == userId && !n.IsRead && n.Type == type && n.BizId == bizId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, now)
                    .SetProperty(n => n.UpdatedAt, now), cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<int> DeleteByIdForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            // ExecuteDelete 单 SQL 直删（v2.12.0 左滑删除）；UserId 条件天然防越权删他人消息
            return await _context.Set<Notification>()
                .Where(n => n.Id == id && n.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<int> DeleteReadAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // 只删已读（v2.12.0 清空已读）：未读保留，避免批量操作吞掉没看过的消息
            return await _context.Set<Notification>()
                .Where(n => n.UserId == userId && n.IsRead)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
