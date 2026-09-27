using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Specifications;

namespace OpenFindBearings.Domain.Repositories
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
        Task<PagedResult<Bearing>> SearchAsync(
            BearingSearchParams searchParams,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取轴承总数（用于分页）
        /// </summary>
        Task<int> GetTotalCountAsync(
            BearingSearchParams searchParams,
            CancellationToken cancellationToken = default);

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
        Task<bool> ExistsByPartNumberAsync(string partNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// 随机取 N 个"有图片"的轴承（游戏中心连连看题库用）。
        /// 改动说明（v2.10.1）：按类型打散优先取——同类型轴承照片高度相似，
        /// 视觉难辨会让连连看变成找不同，跨类型混搭保证一眼可辨。
        /// </summary>
        Task<IReadOnlyList<Bearing>> GetRandomWithImageAsync(int count, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取热门轴承
        /// </summary>
        Task<IEnumerable<Bearing>> GetHotBearingsAsync(int count, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有轴承（用于统计）
        /// </summary>
        Task<IEnumerable<Bearing>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除轴承（软删除）
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取各轴承类型的轴承数量统计
        /// </summary>
        Task<Dictionary<Guid, int>> GetBearingCountByTypeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取各品牌的轴承数量统计
        /// </summary>
        Task<Dictionary<Guid, int>> GetBearingCountByBrandAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据ID获取轴承（包含已停用的，用于彻底删除前校验）
        /// </summary>
        Task<Bearing?> GetByIdIgnoringFilterAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取指定时间后创建的轴承数量
        /// </summary>
        Task<int> GetCountSinceAsync(DateTime since, CancellationToken cancellationToken = default);

        /// <summary>
        /// 彻底删除轴承（物理删除）
        /// </summary>
        Task RemoveAsync(Bearing bearing, CancellationToken cancellationToken = default);
    }
}
