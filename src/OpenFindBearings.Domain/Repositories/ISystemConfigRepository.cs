using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    /// <summary>
    /// 系统配置仓储接口
    /// </summary>
    public interface ISystemConfigRepository
    {
        /// <summary>
        /// 获取所有系统配置
        /// </summary>
        Task<List<SystemConfig>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据键获取配置值
        /// </summary>
        Task<SystemConfig?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取配置值（泛型）
        /// </summary>
        Task<T?> GetValueAsync<T>(string key, T? defaultValue = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新系统配置
        /// </summary>
        Task UpdateAsync(SystemConfig config, CancellationToken cancellationToken = default);
    }
}
