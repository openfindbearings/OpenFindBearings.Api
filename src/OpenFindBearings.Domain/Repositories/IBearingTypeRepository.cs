using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
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
        /// 获取所有轴承类型（包含已停用的）
        /// </summary>
        Task<List<BearingType>> GetAllIncludingInactiveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加轴承类型
        /// </summary>
        Task AddAsync(BearingType bearingType, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新轴承类型
        /// </summary>
        Task UpdateAsync(BearingType bearingType, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据ID获取轴承类型（包含已停用的，用于恢复操作）
        /// </summary>
        Task<BearingType?> GetByIdIgnoringFilterAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 彻底删除轴承类型（物理删除）
        /// </summary>
        Task RemoveAsync(BearingType bearingType, CancellationToken cancellationToken = default);
    }
}
