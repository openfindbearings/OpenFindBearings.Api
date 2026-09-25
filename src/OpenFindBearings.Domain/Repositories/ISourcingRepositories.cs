using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    /// <summary>
    /// 寻货需求仓储接口（v1.35.0）
    /// </summary>
    public interface ISourcingDemandRepository
    {
        /// <summary>按 ID 取需求</summary>
        Task<SourcingDemand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>新增需求</summary>
        Task AddAsync(SourcingDemand demand, CancellationToken cancellationToken = default);

        /// <summary>标记变更</summary>
        Task UpdateAsync(SourcingDemand demand, CancellationToken cancellationToken = default);

        /// <summary>
        /// feed 分页：状态过滤（null=进行中全部）+ 型号关键词（模糊）+ 未应答优先可选，时间倒序
        /// </summary>
        Task<(List<SourcingDemand> Items, int Total)> GetListAsync(int? status, string? keyword, bool onlyOpen,
            int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>我发布的（含全部状态，时间倒序）</summary>
        Task<List<SourcingDemand>> GetByPublisherAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>今日已发布数（免费额度判定；含取消单，防"发了删删了发"绕额度）</summary>
        Task<int> CountPublishedTodayAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>批量惰性过期：进行中且已过 ExpiryAt 的转 Expired，返回受影响行（feed/详情读时兜底）</summary>
        Task<int> ExpireOverdueAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 寻货应答仓储接口（v1.35.0）
    /// </summary>
    public interface ISourcingResponseRepository
    {
        /// <summary>按 ID 取应答</summary>
        Task<SourcingResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>新增应答</summary>
        Task AddAsync(SourcingResponse response, CancellationToken cancellationToken = default);

        /// <summary>标记变更</summary>
        Task UpdateAsync(SourcingResponse response, CancellationToken cancellationToken = default);

        /// <summary>某需求的全部应答（时间正序，feed 详情展示）</summary>
        Task<List<SourcingResponse>> GetByDemandAsync(Guid demandId, CancellationToken cancellationToken = default);

        /// <summary>商户的同需求既有应答（重复应答=更新语义判定）</summary>
        Task<SourcingResponse?> GetByDemandAndMerchantAsync(Guid demandId, Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>我的应答列表（商家维度，时间倒序）</summary>
        Task<List<SourcingResponse>> GetByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>商户今日应答数（免费额度判定）</summary>
        Task<int> CountRespondedTodayAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>需求下全部待处理应答（关闭时批量转 NotSelected / 通知全体应答者）</summary>
        Task<List<SourcingResponse>> GetPendingByDemandAsync(Guid demandId, CancellationToken cancellationToken = default);
    }
}
