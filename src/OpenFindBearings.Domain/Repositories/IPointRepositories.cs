using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    /// <summary>
    /// 积分账户仓储接口（v1.32.0 积分底座）
    /// </summary>
    public interface IPointAccountRepository
    {
        /// <summary>按用户 ID 取账户</summary>
        Task<PointAccount?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>新增账户</summary>
        Task AddAsync(PointAccount account, CancellationToken cancellationToken = default);

        /// <summary>标记变更（提交由 UnitOfWork 管道完成）</summary>
        Task UpdateAsync(PointAccount account, CancellationToken cancellationToken = default);

        /// <summary>删除用户积分账户（v1.34.0 注销清零：人走账销，重注册即新人）</summary>
        Task<int> DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 积分流水仓储接口（v1.32.0）
    /// </summary>
    public interface IPointTransactionRepository
    {
        /// <summary>新增流水</summary>
        Task AddAsync(PointTransaction transaction, CancellationToken cancellationToken = default);

        /// <summary>幂等预检：BizId 是否已存在（唯一索引为最终防线，此为快速路径）</summary>
        Task<bool> ExistsBizIdAsync(string bizId, CancellationToken cancellationToken = default);

        /// <summary>统计用户当日某动作累计发放分值（日上限守卫用）</summary>
        Task<int> SumTodayByTypeAsync(Guid userId, string grantType, CancellationToken cancellationToken = default);

        /// <summary>用户流水分页（时间倒序）</summary>
        Task<(List<PointTransaction> Items, int Total)> GetByUserAsync(Guid userId, int page, int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 用户有流水记录的动作类型集合（since 非空=仅统计该时刻之后，任务中心完成态判定用）
        /// 改动说明（v1.33.0）：一次查询取回类型集合供批量判任务，避免逐规则 Exists 的 N+1
        /// </summary>
        Task<HashSet<string>> GetGrantTypesAsync(Guid userId, DateTime? since, CancellationToken cancellationToken = default);

        /// <summary>删除用户全部流水（v1.32.0，注销匿名化级联清理）</summary>
        Task<int> DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 积分规则仓储接口（v1.32.0）
    /// </summary>
    public interface IPointGrantRuleRepository
    {
        /// <summary>按动作类型取启用中的规则（停用/不存在返回 null）</summary>
        Task<PointGrantRule?> GetEnabledByTypeAsync(string grantType, CancellationToken cancellationToken = default);

        /// <summary>全部规则（Admin 配置页列表）</summary>
        Task<List<PointGrantRule>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>新增规则</summary>
        Task AddAsync(PointGrantRule rule, CancellationToken cancellationToken = default);

        /// <summary>标记变更</summary>
        Task UpdateAsync(PointGrantRule rule, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 一次性奖励认领台账仓储（v1.34.0 防刷）：BizKey 唯一键的原子认领
    /// </summary>
    public interface IPointRewardClaimRepository
    {
        /// <summary>
        /// 尝试认领（INSERT ON CONFLICT DO NOTHING 原子操作）：
        /// 返回 true=本次首次认领（应发奖）；false=该键历史已被认领（跳过发奖）。
        /// 改动说明：走原生 SQL 而非 EF Add+SaveChanges——唯一冲突不污染外层
        /// 变更跟踪器/事务，且天然并发安全（两请求同键只有一个 INSERT 生效）
        /// </summary>
        /// <param name="bizKey">号/照维度幂等键</param>
        /// <param name="grantType">奖励动作类型</param>
        /// <param name="userId">认领人（审计快照）</param>
        Task<bool> TryClaimAsync(string bizKey, string grantType, Guid userId, CancellationToken cancellationToken = default);
    }
}
