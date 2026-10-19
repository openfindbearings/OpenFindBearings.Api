using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Specifications;

namespace OpenFindBearings.Domain.Repositories
{
    /// <summary>
    /// 商家轴承关联仓储接口
    /// 负责商家和轴承之间关联关系的操作
    /// </summary>
    public interface IMerchantBearingRepository
    {
        /// <summary>
        /// 根据ID获取关联
        /// </summary>
        Task<MerchantBearing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据轴承ID获取关联列表
        /// </summary>
        Task<IEnumerable<MerchantBearing>> GetByBearingAsync(Guid bearingId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据商家ID获取关联列表
        /// </summary>
        Task<IEnumerable<MerchantBearing>> GetByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取商家在售的轴承列表
        /// </summary>
        Task<IEnumerable<MerchantBearing>> GetOnSaleByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取商家"可匹配寻货"的关联列表（v1.8.1）：审核通过 且（出售中 或 补货中）且未下架——
        /// 与 <see cref="OpenFindBearings.Domain.Entities.MerchantBearing.IsPubliclyVisible"/> 及寻货匹配口径一致。
        /// 改动说明：寻货"为你匹配/需求信号"原先误用只在售口径的 GetOnSaleByMerchantAsync，
        /// 导致补货中商家被通知却在聚合面看不到，故独立出此方法（不改动在售口径方法的既有调用方）
        /// </summary>
        Task<IEnumerable<MerchantBearing>> GetMatchableByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 商家"公域可见"商品数量（v1.5.0 寻货比价视图"实力摘要"：发布人选定前的供货实力凭证）。
        /// 改动说明（v1.8.2 在售口径统一）：口径由"仅出售中"改为"出售中+补货中（公域可见）"——
        /// 与该商家等级定级、寻货可匹配口径一致，避免同一商家"能被寻货匹配却不计入在售数"的展示矛盾
        /// </summary>
        Task<int> CountPubliclyVisibleAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取待审核的关联列表
        /// </summary>
        Task<IEnumerable<MerchantBearing>> GetPendingApprovalAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查是否存在关联
        /// </summary>
        Task<bool> ExistsAsync(Guid merchantId, Guid bearingId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查轴承是否属于商家
        /// </summary>
        Task<bool> IsOwnedByMerchantAsync(Guid bearingId, Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加关联
        /// </summary>
        Task AddAsync(MerchantBearing merchantBearing, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量添加关联
        /// </summary>
        Task AddRangeAsync(IEnumerable<MerchantBearing> merchantBearings, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新关联
        /// </summary>
        Task UpdateAsync(MerchantBearing merchantBearing, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除关联
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取待审核关联数量
        /// </summary>
        Task<int> GetPendingApprovalCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 分页查询商家的轴承关联（数据库级过滤与分页；reviewStatus 精确过滤审核位，v2.15.0）
        /// </summary>
        Task<PagedResult<MerchantBearing>> GetMerchantBearingsPagedAsync(Guid merchantId, bool? onlyOnSale, bool? pendingOnly, ListingReviewStatus? reviewStatus, DataSourceType? dataSource, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除商户全部商品关联（v2.16.0 认领/提名接管：互联网来源在售数据不随接管继承）
        /// </summary>
        Task<int> DeleteByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>v2.6.0 集体任务 products 指标：本周期内新创建的产品关联数</summary>
        Task<int> CountCreatedSinceAsync(Guid merchantId, DateTime sinceUtc, CancellationToken cancellationToken = default);

        /// <summary>
        /// 商城货源流分页搜索（v2.14.0 商城导购）：全站在售货源，DB 级过滤/排序/分页
        /// </summary>
        Task<PagedResult<MerchantBearing>> SearchMallFeedAsync(MallFeedSearchParams searchParams, CancellationToken cancellationToken = default);
    }
}
