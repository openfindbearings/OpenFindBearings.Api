using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    public class ApiCallLogRepository : IApiCallLogRepository
    {
        private readonly ApplicationDbContext _context;

        public ApiCallLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ApiCallLog log, CancellationToken cancellationToken = default)
        {
            await _context.ApiCallLogs.AddAsync(log, cancellationToken);
        }

        public async Task<int> GetCountByUserIdAsync(Guid userId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
        {
            return await _context.ApiCallLogs
                .Where(l => l.UserId == userId && l.CreatedAt >= startTime && l.CreatedAt <= endTime)
                .CountAsync(cancellationToken);
        }

        public async Task<int> GetCountByIpAsync(string ip, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
        {
            return await _context.ApiCallLogs
                .Where(l => l.ClientIp == ip && l.CreatedAt >= startTime && l.CreatedAt <= endTime)
                .CountAsync(cancellationToken);
        }

        public async Task<int> GetCountBySessionIdAsync(string sessionId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
        {
            return await _context.ApiCallLogs
                .Where(l => l.SessionId == sessionId && l.CreatedAt >= startTime && l.CreatedAt <= endTime)
                .CountAsync(cancellationToken);
        }
    }
}
