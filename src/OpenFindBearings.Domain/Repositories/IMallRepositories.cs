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

        /// <summary>新增商品（v2.4.0 商家挂礼申请入口）</summary>
        Task AddAsync(MallItem item, CancellationToken cancellationToken = default);

        /// <summary>某商户的挂礼列表（v2.4.0，含各审核态，时间倒序）</summary>
        Task<List<MallItem>> GetByOwnerAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>待审商家礼品队列（v2.4.0，Admin 审核定档数据源）</summary>
        Task<List<MallItem>> GetPendingGiftsAsync(CancellationToken cancellationToken = default);

        /// <summary>标记更新（UnitOfWork 统一提交）</summary>
        Task UpdateAsync(MallItem item, CancellationToken cancellationToken = default);

        /// <summary>商户彻底删除时连带硬删其挂礼行（v2.4.0，单条 SQL）</summary>
        Task<int> DeleteByOwnerAsync(Guid merchantId, CancellationToken cancellationToken = default);

        /// <summary>商户退出时全量下架挂礼（v2.4.0 关店释放：保留行供历史订单快照，仅停售）</summary>
        Task<int> OffShelfByOwnerAsync(Guid merchantId, CancellationToken cancellationToken = default);
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

        /// <summary>
        /// 按 ID 连同商品取（v2.4.0 商家发货/结算路径需要归属商户）
        /// </summary>
        Task<(MallOrder? Order, MallItem? Item)> GetWithItemAsync(Guid orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 商家维度订单分页（v2.4.0，含待发货与已发货；可按发货状态过滤，null=全部）
        /// </summary>
        Task<(List<MallOrder> Items, int Total)> GetByOwnerMerchantPagedAsync(
            Guid merchantId, int? shipStatus, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// 同一收货电话或收货地址自指定时间起的有效礼品订单数（v2.4.0 防刷闸：同址同机月限 3 单，
        /// 退款单不计；电话与地址任一命中即计数，取两者较大值防拆分）
        /// </summary>
        Task<int> CountGiftReceiverSinceAsync(string phone, string address, DateTime sinceUtc,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 扫描发货满 N 天仍未确认的实物单（v2.4.0 自动确认收货 Job 数据源，按 ShippedAt 升序限量）
        /// </summary>
        Task<List<MallOrder>> GetAutoConfirmableAsync(DateTime shippedBeforeUtc, int limit,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Admin 托管中实物礼品单（v2.4.0，ShipStatus 1/2 未结算，按创建时间倒序限量；争议退款队列）
        /// </summary>
        Task<List<MallOrder>> GetEscrowGiftOrdersAsync(int limit, CancellationToken cancellationToken = default);
    }
}
