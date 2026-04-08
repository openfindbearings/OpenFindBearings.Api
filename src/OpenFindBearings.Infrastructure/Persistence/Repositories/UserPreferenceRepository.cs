using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 用户偏好仓储实现
    /// </summary>
    public class UserPreferenceRepository : IUserPreferenceRepository
    {
        private readonly ApplicationDbContext _context;

        public UserPreferenceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 根据用户ID获取偏好设置
        /// </summary>
        public async Task<UserPreference?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        }

        /// <summary>
        /// 添加用户偏好设置
        /// </summary>
        public async Task AddAsync(UserPreference preference, CancellationToken cancellationToken = default)
        {
            await _context.UserPreferences.AddAsync(preference, cancellationToken);
            
        }

        /// <summary>
        /// 更新用户偏好设置
        /// </summary>
        public async Task UpdateAsync(UserPreference preference, CancellationToken cancellationToken = default)
        {
            _context.UserPreferences.Update(preference);
            
        }
    }
}
