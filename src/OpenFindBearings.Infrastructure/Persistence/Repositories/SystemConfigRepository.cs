using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 系统配置仓储实现
    /// </summary>
    public class SystemConfigRepository : ISystemConfigRepository
    {
        private readonly ApplicationDbContext _context;

        public SystemConfigRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SystemConfig?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            return await _context.SystemConfigs
                .FirstOrDefaultAsync(sc => sc.Key == key, cancellationToken);
        }

        public async Task<List<SystemConfig>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SystemConfigs
                .OrderBy(sc => sc.Group)
                .ThenBy(sc => sc.Key)
                .ToListAsync(cancellationToken);
        }

        public async Task UpdateAsync(SystemConfig config, CancellationToken cancellationToken = default)
        {
            _context.SystemConfigs.Update(config);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
