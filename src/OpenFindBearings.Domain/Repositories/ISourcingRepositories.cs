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
        /// feed 分页：状态过滤（null=进行中全部）+ 型号关键词（模糊）+ 品牌/地区筛选（包含匹配，v1.4.0 大厅筛选），
        /// newestFirst=发布时间降序（默认最新）/升序（最早）；pinFirst=true 时有效置顶排前（v2.10.0，仅公开大厅启用）；
        /// publicOnly=true 时仅公开招标单（排除邀标单，v1.8.0 公开招标/邀标：邀标单不进公开大厅）；
        /// includeDeleted=true 时纳入软删单（v1.8.2：仅 Admin 治理列表使用，让管理员事后仍可审计已删单）
        /// </summary>
        Task<(List<SourcingDemand> Items, int Total)> GetListAsync(int? status, string? keyword, bool onlyOpen,
            int page, int pageSize, bool pinFirst = false, string? brand = null, string? region = null,
            bool newestFirst = true, bool publicOnly = false, bool includeDeleted = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 按发布人 + 幂等键取单（v1.8.2 彻底幂等）：重复提交（网络重试/双击）命中即返回原单，
        /// 不再建单、不再扣分；key 为空时调用方不应查此方法
        /// </summary>
        Task<SourcingDemand?> GetByPublisherAndKeyAsync(Guid publisherUserId, string idempotencyKey,
            CancellationToken cancellationToken = default);

        /// <summary>我发布的（含全部状态，时间倒序）。改动说明（v2.12.0 商户名义发布）：
        /// 仅个人名义单——商户名义的归商户工作台（GetByPublisherMerchantAsync），双体系不混排</summary>
        Task<List<SourcingDemand>> GetByPublisherAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>商户名义发布的单（v2.12.0 商户工作台"寻货管理-我发布的"，含全部状态，时间倒序）</summary>
        Task<List<SourcingDemand>> GetByPublisherMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>邀标给某商户的进行中需求（v1.8.0 公开招标/邀标：商家侧"邀标给我的"，时间倒序）</summary>
        Task<List<SourcingDemand>> GetOpenInvitedForMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default);

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

        /// <summary>
        /// 某需求的全部应答（时间正序，feed 详情展示）。
        /// 改动说明（v1.8.2 撤销应答软取消）：排除已撤销（Cancelled）行——撤销的应答不参与比价展示
        /// </summary>
        Task<List<SourcingResponse>> GetByDemandAsync(Guid demandId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 商户的同需求既有应答（重复应答=更新语义判定）。
        /// 改动说明（v1.8.2 撤销应答软取消）：**包含**已撤销行——它同时是 (DemandId,MerchantId) 唯一键的查找口，
        /// 撤销后重新应答须复用原行（Revive），故此处不得排除 Cancelled
        /// </summary>
        Task<SourcingResponse?> GetByDemandAndMerchantAsync(Guid demandId, Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 商户今日应答条数（额度评估用，商户维度）。
        /// 改动说明（v1.8.2 撤销应答软取消）：按落库行数统计、不限状态——撤销不再回退额度，
        /// "撤-重答"无法绕开每日额度（撤销行仍在，当日额度一次消耗即为消耗）
        /// </summary>
        Task<int> CountRespondedTodayAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 操作人今日新增应答条数（v2.7.0 G2 每日三件套判定用：次日刷新，重复更新不计新增）
        /// </summary>
        Task<int> CountRespondedTodayByUserAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 我的应答列表（商家维度，时间倒序）。
        /// 改动说明（v1.8.2 撤销应答软取消）：排除已撤销行（撤销即从我方列表消失，等价原删行语义）
        /// </summary>
        Task<List<SourcingResponse>> GetByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>需求下全部待处理应答（关闭时批量转 NotSelected / 通知全体应答者）</summary>
        Task<List<SourcingResponse>> GetPendingByDemandAsync(Guid demandId, CancellationToken cancellationToken = default);

        /// <summary>应答的型号行（v1.5.0 多行标书，比价视图逐条拉取）</summary>
        Task<List<SourcingResponseItem>> GetItemsAsync(Guid responseId, CancellationToken cancellationToken = default);

        /// <summary>清空应答的型号行（v1.5.0 重复应答整体替换行）</summary>
        Task DeleteItemsAsync(Guid responseId, CancellationToken cancellationToken = default);

        /// <summary>新增应答型号行（v1.5.0 显式 Add，规避 EF 导航陷阱）</summary>
        Task AddItemsAsync(IEnumerable<SourcingResponseItem> items, CancellationToken cancellationToken = default);
    }
}
