using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
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

        // 改动说明（v2.17.0 僵尸方法清理）：删除 GetByTargetBearingAsync——反向关系由 GetForBearingAsync
        //   （源 ∪ 双向反向合并）覆盖，原方法已无任何调用方

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
        /// 删除替代关系（物理删除）
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取某轴承的全部替代关系（v2.17.0 字典化：进 API 即生效，无审核态），按"源∪双向反向"合并——
        /// 该轴承作为源（正向：目标可替代我）或作为目标且标记双向（反向：我可替代目标）均返回。
        /// 用于轴承详情替代品展示，让 IsBidirectional 真正生效
        /// </summary>
        Task<List<BearingInterchange>> GetForBearingAsync(Guid bearingId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 分页获取替代关系（平台管理列表）：可按关键字（源/目标型号）与创建时间、来源过滤。
        /// 改动说明（v2.17.0 字典化）：去掉审核状态参数（状态机已取消）
        /// </summary>
        Task<(List<BearingInterchange> Items, int Total)> GetPagedAsync(
            string? keyword,
            DateTime? createdFromUtc, DateTime? createdToUtc, bool? internetSource,
            int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// 替代关系总数（v1.50.0 仪表盘统计）
        /// </summary>
        Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取指定时间后新增、且按来源区分的替代关系数（v1.50.0 来源拆分统计）
        /// </summary>
        /// <param name="since">起始时间（UTC，含）</param>
        /// <param name="isInternet">true=互联网来源（Sync 批量）；false=人工来源（平台维护）</param>
        Task<int> GetCountSinceBySourceAsync(DateTime since, bool isInternet, CancellationToken cancellationToken = default);
    }
}
