using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Interfaces;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 纠错请求仓储实现
    /// </summary>
    public class CorrectionRequestRepository : ICorrectionRequestRepository
    {
        private readonly AppDbContext _context;

        public CorrectionRequestRepository(AppDbContext context)
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
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(CorrectionRequest correction, CancellationToken cancellationToken = default)
        {
            _context.CorrectionRequests.Update(correction);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<CorrectionRequest>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.CorrectionRequests
                .Include(c => c.Submitter)
                .Include(c => c.Reviewer)
                .OrderByDescending(c => c.SubmittedAt)
                .ToListAsync(cancellationToken);
        }
    }
}
