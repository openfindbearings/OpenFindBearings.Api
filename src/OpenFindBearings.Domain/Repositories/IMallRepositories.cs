using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Domain.Repositories
{
    /// <summary>
    /// 商城商品目录仓储（v2.3.0 商城虚拟权益）：目录读 + Admin 改写。
    /// 价格/闪购窗口实时读不缓存——Admin 改价即时生效无需发版
    /// </summary>
    public interface IMallItemRepository
    {
        /// <summary>上架商品（Enabled=true），按 SortOrder 升序</summary>
        Task<List<MallItem>> GetEnabledAsync(CancellationToken cancellationToken = default);

        /// <summary>全量目录（含停用，Admin 管理页数据源）</summary>
        Task<List<MallItem>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>按 ID 取（兑换/编辑路径，事务内重读用）</summary>
        Task<MallItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>标记更新（UnitOfWork 统一提交）</summary>
        Task UpdateAsync(MallItem item, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 商城订单仓储（v2.3.0）：兑换凭据写入 + 我的订单分页 + 注销级联清理
    /// </summary>
    public interface IMallOrderRepository
    {
        /// <summary>新增订单（与扣分同事务）</summary>
        Task AddAsync(MallOrder order, CancellationToken cancellationToken = default);

        /// <summary>我的订单分页（时间倒序）</summary>
        Task<(List<MallOrder> Items, int Total)> GetByUserPagedAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>按 ID 取（Admin 退款/争议处理）</summary>
        Task<MallOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>标记更新（履约状态推进）</summary>
        Task UpdateAsync(MallOrder order, CancellationToken cancellationToken = default);

        /// <summary>删除用户全部订单（注销清理级联，单条 SQL）</summary>
        Task<int> DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
