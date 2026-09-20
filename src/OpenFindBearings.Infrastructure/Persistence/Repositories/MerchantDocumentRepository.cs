using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    public class MerchantDocumentRepository : IMerchantDocumentRepository
    {
        private readonly ApplicationDbContext _context;

        public MerchantDocumentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<MerchantDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.MerchantDocuments
                .Include(l => l.Merchant)
                .Include(l => l.Submitter)
                .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        }

        /// <summary>
        /// 入驻后材料变更队列：仅取待审材料，联载商家与提交人（队列列显示依赖），
        /// 并排除入驻申请审核中（Pending）商户——随单材料在"入驻申请审批"抽屉内审结，
        /// 双入口会造成审核人重复操作（v1.5.1 修复）
        /// </summary>
        public async Task<PagedResult<MerchantDocument>> GetPendingAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.MerchantDocuments
                .Include(l => l.Merchant)
                .Include(l => l.Submitter)
                .Where(l => l.Status == DocumentStatus.Pending
                    && l.Merchant!.Status != MerchantStatus.Pending)
                .OrderByDescending(l => l.SubmittedAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<MerchantDocument>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<MerchantDocument>> GetByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default)
        {
            return await _context.MerchantDocuments
                .Where(l => l.MerchantId == merchantId)
                .OrderByDescending(l => l.SubmittedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(MerchantDocument document, CancellationToken cancellationToken = default)
        {
            await _context.MerchantDocuments.AddAsync(document, cancellationToken);
        }

        public async Task UpdateAsync(MerchantDocument document, CancellationToken cancellationToken = default)
        {
            _context.MerchantDocuments.Update(document);
        }

        /// <summary>待审材料数（仪表盘角标），与变更队列同口径：排除入驻审核中商户的随单材料</summary>
        public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.MerchantDocuments
                .CountAsync(l => l.Status == DocumentStatus.Pending
                    && l.Merchant!.Status != MerchantStatus.Pending, cancellationToken);
        }
    }
}
