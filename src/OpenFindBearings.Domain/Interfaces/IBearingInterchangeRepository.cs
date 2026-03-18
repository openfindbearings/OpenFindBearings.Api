using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Interfaces
{
    /// <summary>
    /// 轴承替代品仓储接口
    /// 负责轴承替代品关系的持久化操作
    /// </summary>
    public interface IBearingInterchangeRepository
    {
        /// <summary>
        /// 根据ID获取替代关系
        /// </summary>
        Task<BearingInterchange?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据源轴承ID获取替代关系列表
        /// </summary>
        Task<List<BearingInterchange>> GetBySourceBearingAsync(Guid bearingId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据目标轴承ID获取替代关系列表
        /// </summary>
        Task<List<BearingInterchange>> GetByTargetBearingAsync(Guid bearingId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查替代关系是否存在
        /// </summary>
        Task<bool> ExistsAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加替代关系
        /// </summary>
        Task AddAsync(BearingInterchange interchange, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量添加替代关系
        /// </summary>
        Task AddRangeAsync(List<BearingInterchange> interchanges, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新替代关系
        /// </summary>
        Task UpdateAsync(BearingInterchange interchange, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除替代关系
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
