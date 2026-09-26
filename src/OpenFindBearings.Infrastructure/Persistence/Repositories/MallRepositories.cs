using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 商城商品目录仓储实现（v2.3.0）：实时读不缓存，Admin 改价即时生效
    /// </summary>
    public class MallItemRepository : IMallItemRepository
    {
        private readonly ApplicationDbContext _context;

        public MallItemRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public Task<List<MallItem>> GetEnabledAsync(CancellationToken cancellationToken = default)
        {
            return _context.Set<MallItem>()
                .Where(i => i.Enabled && i.IsActive)
                .OrderBy(i => i.SortOrder)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<List<MallItem>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return _context.Set<MallItem>()
                .OrderBy(i => i.SortOrder)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<MallItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _context.Set<MallItem>().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public Task UpdateAsync(MallItem item, CancellationToken cancellationToken = default)
        {
            _context.Set<MallItem>().Update(item);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 商城订单仓储实现（v2.3.0）：兑换凭据写入/我的订单分页/注销级联清理
    /// </summary>
    public class MallOrderRepository : IMallOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public MallOrderRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task AddAsync(MallOrder order, CancellationToken cancellationToken = default)
        {
            await _context.Set<MallOrder>().AddAsync(order, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<(List<MallOrder> Items, int Total)> GetByUserPagedAsync(
            Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<MallOrder>().Where(o => o.UserId == userId && o.IsActive);
            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            return (items, total);
        }

        /// <inheritdoc/>
        public Task<MallOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _context.Set<MallOrder>().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public Task UpdateAsync(MallOrder order, CancellationToken cancellationToken = default)
        {
            _context.Set<MallOrder>().Update(order);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<int> DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<MallOrder>()
                .Where(o => o.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
