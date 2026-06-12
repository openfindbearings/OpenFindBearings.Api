using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    /// <summary>
    /// 品牌仓储接口
    /// 对应接口：GET /api/brands
    /// </summary>
    public interface IBrandRepository
    {
        /// <summary>
        /// 根据ID获取品牌
        /// </summary>
        Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据代码获取品牌
        /// </summary>
        Task<Brand?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有品牌
        /// </summary>
        Task<List<Brand>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有品牌（包含已停用的）
        /// </summary>
        Task<List<Brand>> GetAllIncludingInactiveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加品牌
        /// </summary>
        Task AddAsync(Brand brand, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新品牌
        /// </summary>
        Task UpdateAsync(Brand brand, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据ID获取品牌（包含已停用的，用于恢复操作）
        /// </summary>
        Task<Brand?> GetByIdIgnoringFilterAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 彻底删除品牌（物理删除）
        /// </summary>
        Task RemoveAsync(Brand brand, CancellationToken cancellationToken = default);
    }
}
