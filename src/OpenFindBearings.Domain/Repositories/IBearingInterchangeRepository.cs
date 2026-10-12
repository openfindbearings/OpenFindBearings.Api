using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;

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
        /// 删除替代关系（物理删除）
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取某轴承的公域可见替代关系（v2.16.0）：仅"已生效"，且按"源∪双向反向"合并——
        /// 该轴承作为源（正向：目标可替代我）或作为目标且标记双向（反向：我可替代目标）均返回。
        /// 用于轴承详情替代品展示，让 IsBidirectional 真正生效
        /// </summary>
        Task<List<BearingInterchange>> GetApprovedForBearingAsync(Guid bearingId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 分页获取替代关系（平台管理列表）：可按关键字（源/目标型号）与审核状态过滤
        /// </summary>
        Task<(List<BearingInterchange> Items, int Total)> GetPagedAsync(
            string? keyword, InterchangeReviewStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// 分页获取待审核替代关系（平台审核队列）
        /// </summary>
        Task<(List<BearingInterchange> Items, int Total)> GetPendingAsync(
            int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取某商户提交的替代候选（含各审核状态，供商家查看进度与驳回理由）
        /// </summary>
        Task<List<BearingInterchange>> GetByProposerAsync(Guid merchantId, CancellationToken cancellationToken = default);
    }
}
