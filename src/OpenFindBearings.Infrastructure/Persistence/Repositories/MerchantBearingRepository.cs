using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 商家轴承关联仓储实现
    /// </summary>
    public class MerchantBearingRepository : IMerchantBearingRepository
    {
        private readonly ApplicationDbContext _context;

        public MerchantBearingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<MerchantBearing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.MerchantBearings
                .Include(mb => mb.Merchant)
                .Include(mb => mb.Bearing)
                .FirstOrDefaultAsync(mb => mb.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<MerchantBearing>> GetByBearingAsync(Guid bearingId, CancellationToken cancellationToken = default)
        {
            return await _context.MerchantBearings
                .Include(mb => mb.Merchant)
                .Where(mb => mb.BearingId == bearingId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<MerchantBearing>> GetByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.MerchantBearings
                .Include(mb => mb.Bearing)
                    .ThenInclude(b => b.Brand)
                .Include(mb => mb.Bearing)
                    .ThenInclude(b => b.BearingType)
                .Where(mb => mb.MerchantId == merchantId)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 获取商家在售的轴承列表
        /// </summary>
        public async Task<IEnumerable<MerchantBearing>> GetOnSaleByMerchantAsync(
            Guid merchantId,
            CancellationToken cancellationToken = default)
        {
            return await _context.MerchantBearings
                .Include(mb => mb.Bearing)
                    .ThenInclude(b => b.Brand)
                .Include(mb => mb.Bearing)
                    .ThenInclude(b => b.BearingType)
                .Where(mb => mb.MerchantId == merchantId && mb.IsOnSale)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 获取待审核的关联列表
        /// </summary>
        public async Task<IEnumerable<MerchantBearing>> GetPendingApprovalAsync(CancellationToken cancellationToken = default)
        {
            return await _context.MerchantBearings
                .Include(mb => mb.Merchant)
                .Include(mb => mb.Bearing)
                .Where(mb => mb.IsPendingApproval)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 检查是否存在关联
        /// </summary>
        public async Task<bool> ExistsAsync(Guid merchantId, Guid bearingId, CancellationToken cancellationToken = default)
        {
            return await _context.MerchantBearings
                .AnyAsync(mb => mb.MerchantId == merchantId && mb.BearingId == bearingId, cancellationToken);
        }

        /// <summary>
        /// 检查轴承是否属于商家
        /// </summary>
        public async Task<bool> IsOwnedByMerchantAsync(Guid bearingId, Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.MerchantBearings
                .AnyAsync(mb => mb.BearingId == bearingId && mb.MerchantId == merchantId, cancellationToken);
        }

        public async Task AddAsync(MerchantBearing merchantBearing, CancellationToken cancellationToken = default)
        {
            await _context.MerchantBearings.AddAsync(merchantBearing, cancellationToken);
            
        }

        /// <summary>
        /// 批量添加关联
        /// </summary>
        public async Task AddRangeAsync(
            IEnumerable<MerchantBearing> merchantBearings,
            CancellationToken cancellationToken = default)
        {
            await _context.MerchantBearings.AddRangeAsync(merchantBearings, cancellationToken);
            
        }

        public async Task UpdateAsync(MerchantBearing merchantBearing, CancellationToken cancellationToken = default)
        {
            _context.MerchantBearings.Update(merchantBearing);
            
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var merchantBearing = await GetByIdAsync(id, cancellationToken);
            if (merchantBearing != null)
            {
                _context.MerchantBearings.Remove(merchantBearing);
                
            }
        }
    }
}
