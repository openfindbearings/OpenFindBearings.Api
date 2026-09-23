using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 纠错请求仓储实现
    /// </summary>
    public class CorrectionRequestRepository : ICorrectionRequestRepository
    {
        private readonly ApplicationDbContext _context;

        public CorrectionRequestRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CorrectionRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.CorrectionRequests
                .Include(c => c.Submitter)
                .Include(c => c.Reviewer)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        /// <summary>
        /// 获取所有待审核纠错
        /// </summary>
        public async Task<List<CorrectionRequest>> GetPendingAsync(CancellationToken cancellationToken = default)
        {
            return await _context.CorrectionRequests
                .Include(c => c.Submitter)
                .Where(c => c.Status == CorrectionStatus.Pending)
                .OrderBy(c => c.SubmittedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 根据目标类型和目标ID获取纠错列表
        /// </summary>
        public async Task<List<CorrectionRequest>> GetByTargetAsync(
            string targetType,
            Guid targetId,
            CancellationToken cancellationToken = default)
        {
            return await _context.CorrectionRequests
                .Include(c => c.Submitter)
                .Include(c => c.Reviewer)
                .Where(c => c.TargetType == targetType && c.TargetId == targetId)
                .OrderByDescending(c => c.SubmittedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<CorrectionRequest>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.CorrectionRequests
                .Include(c => c.Reviewer)
                .Where(c => c.SubmittedBy == userId)
                .OrderByDescending(c => c.SubmittedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(CorrectionRequest correction, CancellationToken cancellationToken = default)
        {
            await _context.CorrectionRequests.AddAsync(correction, cancellationToken);
        }

        public async Task UpdateAsync(CorrectionRequest correction, CancellationToken cancellationToken = default)
        {
            _context.CorrectionRequests.Update(correction);
        }

        public async Task<List<CorrectionRequest>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.CorrectionRequests
                .Include(c => c.Submitter)
                .Include(c => c.Reviewer)
                .OrderByDescending(c => c.SubmittedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetCountByStatusAsync(CorrectionStatus status, CancellationToken cancellationToken = default)
        {
            return await _context.CorrectionRequests
                .CountAsync(c => c.Status == status, cancellationToken);
        }

        public async Task<int> GetCountSinceAsync(DateTime since, CancellationToken cancellationToken = default)
        {
            return await _context.CorrectionRequests
                .CountAsync(c => c.CreatedAt >= since, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<int> DeleteByTargetAsync(string targetType, Guid targetId, CancellationToken cancellationToken = default)
        {
            // v2.17.0：商户删除/解除归属前必须硬删其纠错行——TargetId 上挂 Merchants Restrict FK，
            //   驳回/保留行都会让商户 DELETE 被 23503 拦截（withdraw/删被拒申请/注销同雷共享修复）
            return await _context.CorrectionRequests
                .Where(c => c.TargetType == targetType && c.TargetId == targetId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<int> DeleteByUserAsync(Guid userId, CorrectionStatus? status = null, CancellationToken cancellationToken = default)
        {
            // v2.17.0：注销清本人待审纠错（防 Admin 事后采纳给幽灵用户发通知，status=Pending）；
            //   匿名化清全部历史（status=null 全删，个保法个人数据删除义务）
            return await _context.CorrectionRequests
                .Where(c => c.SubmittedBy == userId && (status == null || c.Status == status))
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
