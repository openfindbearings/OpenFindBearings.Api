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
        /// 兑换（扣分+建单+履约同链路）。requestId 为客户端幂等键（同一次确认重复提交只扣一次）
        /// </summary>
        Task<RedeemResult> RedeemAsync(Guid userId, Guid itemId, Guid? targetRef, string? requestId,
            CancellationToken cancellationToken = default);

        /// <summary>我的订单分页（时间倒序）</summary>
        Task<(List<MallOrderItem> Items, int Total)> GetMyOrdersAsync(Guid userId, int page, int pageSize,
            CancellationToken cancellationToken = default);
    }

    /// <summary>目录条目视图（价格已按闪购窗口结算，前端无需再判时间）</summary>
    public record MallCatalogItem(
        Guid Id, string Key, string Name, string Description, string Icon,
        int Category, int Price, int? OriginalPrice, bool Flashing, DateTime? FlashEnd,
        int? DurationHours, int Stock, int SoldCount, bool SoldOut);

    /// <summary>目录结果（含余额，供"积分不足去赚"三态按钮）</summary>
    public record MallCatalogResult(List<MallCatalogItem> Items, int Balance);

    /// <summary>兑换结果（Success=false 时 Message 为用户可读原因）</summary>
    public record RedeemResult(bool Success, string? Message, Guid? OrderId, int PointsSpent, DateTime? PinnedUntil);

    /// <summary>订单条目视图</summary>
    public record MallOrderItem(
        Guid Id, string ItemKey, string ItemName, int PointsSpent, int Status,
        string? Remark, DateTime CreatedAt, DateTime? FulfilledAt);
}
