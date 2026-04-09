using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    public class SystemConfigRepository : ISystemConfigRepository
    {
        private readonly ApplicationDbContext _context;

        public SystemConfigRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<SystemConfig>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SystemConfigs.ToListAsync(cancellationToken);
        }

        public async Task<SystemConfig?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            return await _context.SystemConfigs.FirstOrDefaultAsync(c => c.Key == key, cancellationToken);
        }

        /// <summary>
        /// 获取配置值（泛型）
        /// </summary>
        public async Task<T?> GetValueAsync<T>(string key, T? defaultValue = default, CancellationToken cancellationToken = default)
        {
            var config = await GetByKeyAsync(key, cancellationToken);
            if (config == null || string.IsNullOrEmpty(config.Value))
                return defaultValue;

            try
            {
                return (T)Convert.ChangeType(config.Value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        public async Task UpdateAsync(SystemConfig config, CancellationToken cancellationToken = default)
        {
            _context.SystemConfigs.Update(config);
            
        }
    }
}
