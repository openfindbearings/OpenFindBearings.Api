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
}
