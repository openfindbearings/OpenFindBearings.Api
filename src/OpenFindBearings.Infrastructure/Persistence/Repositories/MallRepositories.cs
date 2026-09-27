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
        public async Task AddAsync(MallItem item, CancellationToken cancellationToken = default)
        {
            await _context.Set<MallItem>().AddAsync(item, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<List<MallItem>> GetByOwnerAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            return _context.Set<MallItem>()
                .Where(i => i.OwnerMerchantId == merchantId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<List<MallItem>> GetPendingGiftsAsync(CancellationToken cancellationToken = default)
        {
            return _context.Set<MallItem>()
                .Where(i => i.OwnerMerchantId != null && i.AuditState == 1 && i.IsActive)
                .OrderBy(i => i.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task UpdateAsync(MallItem item, CancellationToken cancellationToken = default)
        {
            _context.Set<MallItem>().Update(item);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<int> DeleteByOwnerAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<MallItem>()
                .Where(i => i.OwnerMerchantId == merchantId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<int> OffShelfByOwnerAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            // ExecuteUpdate 批量下架：保留行供历史订单快照，仅停售
            return await _context.Set<MallItem>()
                .Where(i => i.OwnerMerchantId == merchantId && i.Enabled)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.Enabled, false), cancellationToken);
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
        public async Task<(MallOrder? Order, MallItem? Item)> GetWithItemAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            // 订单与商品无导航属性（兑换后目录可改可删，快照独立），分别按主键取
            var order = await _context.Set<MallOrder>().FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
            if (order == null) return (null, null);
            var item = await _context.Set<MallItem>().FirstOrDefaultAsync(i => i.Id == order.ItemId, cancellationToken);
            return (order, item);
        }

        /// <inheritdoc/>
        public async Task<(List<MallOrder> Items, int Total)> GetByOwnerMerchantPagedAsync(
            Guid merchantId, int? shipStatus, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            // 商家礼品订单 = 商品归属该商户的所有订单（经 Item 关联回查，含收货信息供发货）
            var query = _context.Set<MallOrder>()
                .Where(o => o.IsActive)
                .Join(_context.Set<MallItem>().Where(i => i.OwnerMerchantId == merchantId),
                    o => o.ItemId, i => i.Id, (o, i) => o);
            if (shipStatus.HasValue)
                query = query.Where(o => o.ShipStatus == shipStatus.Value);
            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            return (items, total);
        }

        /// <inheritdoc/>
        public async Task<int> CountGiftReceiverSinceAsync(string phone, string address, DateTime sinceUtc,
            CancellationToken cancellationToken = default)
        {
            // 有效礼品单=非退款态（ShipStatus!=4）且电话或地址任一命中；两个计数取较大值防拆分规避
            var byPhone = await _context.Set<MallOrder>()
                .CountAsync(o => o.IsActive && o.ShipStatus != 4 && o.CreatedAt >= sinceUtc && o.ReceiverPhone == phone, cancellationToken);
            var byAddress = await _context.Set<MallOrder>()
                .CountAsync(o => o.IsActive && o.ShipStatus != 4 && o.CreatedAt >= sinceUtc && o.ReceiverAddress == address, cancellationToken);
            return Math.Max(byPhone, byAddress);
        }

        /// <inheritdoc/>
        public Task<List<MallOrder>> GetAutoConfirmableAsync(DateTime shippedBeforeUtc, int limit,
            CancellationToken cancellationToken = default)
        {
            return _context.Set<MallOrder>()
                .Where(o => o.IsActive && o.ShipStatus == 2 && o.ShippedAt != null && o.ShippedAt <= shippedBeforeUtc)
                .OrderBy(o => o.ShippedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<int> DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<MallOrder>()
                .Where(o => o.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<List<MallOrder>> GetEscrowGiftOrdersAsync(int limit, CancellationToken cancellationToken = default)
        {
            // 托管中=待发货(1)/已发货(2)的实物单，未结算，Admin 争议退款队列
            return _context.Set<MallOrder>()
                .Where(o => o.IsActive && (o.ShipStatus == 1 || o.ShipStatus == 2))
                .OrderByDescending(o => o.CreatedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }
    }
}
