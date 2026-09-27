namespace OpenFindBearings.Application.Services
{
    /// <summary>
    /// 商城服务接口（v2.3.0 商城虚拟权益）：目录浏览 + 积分兑换履约 + 我的订单。
    /// 一期仅置顶卡实装履约；其余类别返回"即将上线"而不半截生效。
    /// 兑换路径刻意"先全量校验、后扣分、再履约"，履约异常自动原路退分
    /// </summary>
    public interface IMallService
    {
        /// <summary>商城目录（含生效价/闪购态/库存态）+ 当前用户余额（前端三态按钮依据）</summary>
        Task<MallCatalogResult> GetCatalogAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 兑换（扣分+建单+履约同链路）。requestId 为客户端幂等键（同一次确认重复提交只扣一次）。
        /// useTreasury=true 时改从 targetRef 所属商户金库支出（仅该商户管理员，v2.4.0）
        /// </summary>
        Task<RedeemResult> RedeemAsync(Guid userId, Guid itemId, Guid? targetRef, string? requestId,
            bool useTreasury = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// 兑换商家实物礼品（v2.4.0 挂礼托管）：收货三件套必填；
        /// 防刷闸=自家礼品排除 + 同址同机月限单；扣分托管、待商家发货
        /// </summary>
        Task<RedeemResult> RedeemGiftAsync(Guid userId, Guid itemId, string receiverName, string receiverPhone,
            string receiverAddress, string? requestId, CancellationToken cancellationToken = default);

        /// <summary>确认收货（买家本人）：结算入发布商户金库（月顶内全额，幂等 settle:{订单}）</summary>
        Task<(bool Success, string? Message, int SettledPoints)> ConfirmReceiptAsync(Guid userId, Guid orderId,
            CancellationToken cancellationToken = default);

        /// <summary>商家发货（该礼品归属商户的在职成员）：登记物流单号，起算 7 天自动确认</summary>
        Task<(bool Success, string? Message)> ShipGiftOrderAsync(Guid operatorUserId, Guid orderId, string tracking,
            CancellationToken cancellationToken = default);

        /// <summary>Admin 争议退款（仅未结算订单原路退分；已入金库的走金库侧人工处理，不自动反冲）</summary>
        Task<(bool Success, string? Message)> RefundDisputeAsync(Guid orderId, string reason,
            CancellationToken cancellationToken = default);

        /// <summary>我的订单分页（时间倒序）</summary>
        Task<(List<MallOrderItem> Items, int Total)> GetMyOrdersAsync(Guid userId, int page, int pageSize,
            CancellationToken cancellationToken = default);
    }

    /// <summary>目录条目视图（价格已按闪购窗口结算，前端无需再判时间）</summary>
    public record MallCatalogItem(
        Guid Id, string Key, string Name, string Description, string Icon,
        int Category, int Price, int? OriginalPrice, bool Flashing, DateTime? FlashEnd,
        int? DurationHours, int Stock, int SoldCount, bool SoldOut,
        // v2.4.0 挂礼：归属商户名（实物礼品展示"来自 XX 商家"；平台权益为 null）
        string? OwnerMerchantName = null);

    /// <summary>目录结果（含余额，供"积分不足去赚"三态按钮）</summary>
    public record MallCatalogResult(List<MallCatalogItem> Items, int Balance);

    /// <summary>兑换结果（Success=false 时 Message 为用户可读原因）</summary>
    public record RedeemResult(bool Success, string? Message, Guid? OrderId, int PointsSpent, DateTime? PinnedUntil);

    /// <summary>订单条目视图（v2.4.0 扩展物流态：实物礼品单展示待发货/已发货/已收货）</summary>
    public record MallOrderItem(
        Guid Id, string ItemKey, string ItemName, int PointsSpent, int Status,
        string? Remark, DateTime CreatedAt, DateTime? FulfilledAt,
        int ShipStatus = 0, string? ShipTracking = null, DateTime? ShippedAt = null, DateTime? ReceivedAt = null,
        string? ReceiverName = null, string? ReceiverPhone = null, string? ReceiverAddress = null);
}
