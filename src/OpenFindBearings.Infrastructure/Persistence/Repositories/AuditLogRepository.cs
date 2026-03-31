using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Common.Models;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Interfaces;
using OpenFindBearings.Domain.Specifications;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 审核日志仓储实现
    /// </summary>
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly ApplicationDbContext _context;

        public AuditLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
        {
            await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<AuditLog>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
        {
            return await _context.AuditLogs
                .Include(al => al.Operator)
                .Where(al => al.EntityType == entityType && al.EntityId == entityId)
                .OrderByDescending(al => al.OperatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<PagedResult<AuditLog>> SearchAsync(AuditLogSearchParams searchParams, CancellationToken cancellationToken = default)
        {
            var query = _context.AuditLogs
                .Include(al => al.Operator)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchParams.Action))
                query = query.Where(al => al.Action == searchParams.Action);

            if (!string.IsNullOrWhiteSpace(searchParams.EntityType))
                query = query.Where(al => al.EntityType == searchParams.EntityType);

            if (searchParams.EntityId.HasValue)
                query = query.Where(al => al.EntityId == searchParams.EntityId.Value);

            if (searchParams.OperatorId.HasValue)
                query = query.Where(al => al.OperatorId == searchParams.OperatorId.Value);

            if (searchParams.StartDate.HasValue)
                query = query.Where(al => al.OperatedAt >= searchParams.StartDate.Value);

            if (searchParams.EndDate.HasValue)
                query = query.Where(al => al.OperatedAt <= searchParams.EndDate.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(al => al.OperatedAt)
                .Skip((searchParams.Page - 1) * searchParams.PageSize)
                .Take(searchParams.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<AuditLog>
            {
                Items = items,
                TotalCount = totalCount,
                Page = searchParams.Page,
                PageSize = searchParams.PageSize
            };
        }
    }
}
