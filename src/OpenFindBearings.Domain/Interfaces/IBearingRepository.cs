using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Specifications;

namespace OpenFindBearings.Domain.Interfaces
{
    /// <summary>
    /// 轴承仓储接口
    /// 负责轴承实体的持久化操作
    /// </summary>
    public interface IBearingRepository
    {
        /// <summary>
        /// 根据ID获取轴承
        /// </summary>
        Task<Bearing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据型号获取轴承
        /// </summary>
        Task<Bearing?> GetByPartNumberAsync(string partNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// 搜索轴承
        /// </summary>
        Task<IEnumerable<Bearing>> SearchAsync(BearingSearchParams searchParams, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加轴承
        /// </summary>
        Task AddAsync(Bearing bearing, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新轴承
        /// </summary>
        Task UpdateAsync(Bearing bearing, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查型号是否存在
        /// </summary>
        Task<bool> ExistsAsync(string partNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取热门轴承
        /// </summary>
        Task<IEnumerable<Bearing>> GetHotBearingsAsync(int count, CancellationToken cancellationToken = default);
    }
}
