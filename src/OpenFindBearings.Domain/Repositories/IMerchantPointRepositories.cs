using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    /// <summary>
    /// 商家金库账户仓储（v2.4.0 工会经济）
    /// </summary>
    public interface IMerchantPointAccountRepository
    {
        /// <summary>按商户取金库账户（可空=未开户）</summary>
        Task<MerchantPointAccount?> GetByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>新增账户（首笔入账时懒开户）</summary>
        Task AddAsync(MerchantPointAccount account, CancellationToken cancellationToken = default);

        /// <summary>标记更新（UnitOfWork 统一提交）</summary>
        Task UpdateAsync(MerchantPointAccount account, CancellationToken cancellationToken = default);

        /// <summary>硬删账户（商户彻底删除路径）</summary>
        Task<int> DeleteByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 商家金库流水仓储（v2.4.0）：幂等查重 + 日/月上限汇总 + 明细分页
    /// </summary>
    public interface IMerchantPointTransactionRepository
    {
        /// <summary>新增流水</summary>
        Task AddAsync(MerchantPointTransaction tx, CancellationToken cancellationToken = default);

        /// <summary>幂等：同 BizId 是否已存在</summary>
        Task<bool> ExistsBizIdAsync(string bizId, CancellationToken cancellationToken = default);

        /// <summary>某商户某场景自指定时间（UTC）以来的入账总额（trickle 日/月顶与结算月顶统计）</summary>
        Task<int> SumCreditSinceAsync(Guid merchantId, string grantType, DateTime sinceUtc, CancellationToken cancellationToken = default);

        /// <summary>某商户任意场景自指定时间（UTC）以来的入账总额（v2.6.0 集体任务 treasury 指标）</summary>
        Task<int> SumCreditAnySinceAsync(Guid merchantId, DateTime sinceUtc, CancellationToken cancellationToken = default);

        /// <summary>v2.6.0 工会排行榜：自指定时间起各商户金库入账总额 TOP N（按金额降序）</summary>
        Task<List<(Guid MerchantId, int Total)>> GetTopMerchantsCreditAsync(DateTime sinceUtc, int limit,
            CancellationToken cancellationToken = default);

        /// <summary>金库明细分页（时间倒序）</summary>
        Task<(List<MerchantPointTransaction> Items, int Total)> GetByMerchantPagedAsync(Guid merchantId, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>硬删商户全部流水（商户彻底删除路径）</summary>
        Task<int> DeleteByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default);
    }
}
