using OpenFindBearings.Domain.Common.Models;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Parameters;

namespace OpenFindBearings.Domain.Interfaces
{
    public interface IAuditLogRepository
    {
        Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
        Task<List<AuditLog>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);
        Task<PagedResult<AuditLog>> SearchAsync(AuditLogSearchParams searchParams, CancellationToken cancellationToken = default);
    }
}
