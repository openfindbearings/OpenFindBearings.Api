using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Interfaces
{
    /// <summary>
    /// 轴承类型仓储接口
    /// 对应接口：GET /api/bearing-types
    /// </summary>
    public interface IBearingTypeRepository
    {
        /// <summary>
        /// 根据ID获取轴承类型
        /// </summary>
        Task<BearingType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据代码获取轴承类型
        /// </summary>
        Task<BearingType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有轴承类型
        /// </summary>
        Task<List<BearingType>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加轴承类型
        /// </summary>
        Task AddAsync(BearingType bearingType, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新轴承类型
        /// </summary>
        Task UpdateAsync(BearingType bearingType, CancellationToken cancellationToken = default);
    }
}
