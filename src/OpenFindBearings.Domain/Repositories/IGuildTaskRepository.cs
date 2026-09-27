using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    /// <summary>
    /// 工会集体任务仓储（v2.6.0 M3）：定义表读写 + 完成台账判重。
    /// 指标聚合不在此层——corrections 走积分流水、treasury 走金库流水、products 走商品明细，
    /// 由 GuildTaskService 组合既有仓储查询
    /// </summary>
    public interface IGuildTaskRepository
    {
        /// <summary>启用中的任务定义（按 SortOrder）</summary>
        Task<List<GuildTaskDefinition>> GetEnabledAsync(CancellationToken cancellationToken = default);

        /// <summary>全量任务定义（Admin 管理页）</summary>
        Task<List<GuildTaskDefinition>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>按 ID 取</summary>
        Task<GuildTaskDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>新增任务定义（Admin 创建/迁移种子外的运行期补建）</summary>
        Task AddDefinitionAsync(GuildTaskDefinition definition, CancellationToken cancellationToken = default);

        /// <summary>标记更新</summary>
        Task UpdateDefinitionAsync(GuildTaskDefinition definition, CancellationToken cancellationToken = default);

        /// <summary>某商户在指定周期内已完成的台账（进度卡与结算判重共用）</summary>
        Task<HashSet<string>> GetCompletedTaskKeysAsync(Guid merchantId, string periodKey,
            CancellationToken cancellationToken = default);

        /// <summary>记一笔完成台账（唯一索引冲突抛 DbUpdateException，由服务层吞=已结算）</summary>
        Task AddCompletionAsync(GuildTaskCompletion completion, CancellationToken cancellationToken = default);

        /// <summary>商户完成历史条数（榜单/展示扩展预留可复用；本批用于任务卡"累计完成"）</summary>
        Task<int> CountCompletionsAsync(Guid merchantId, CancellationToken cancellationToken = default);
    }
}
