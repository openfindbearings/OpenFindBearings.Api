using OpenFindBearings.Domain.Abstractions;
using OpenFindBearings.Domain.Aggregates;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Events;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 商家轴承关联实体
    /// 表示某个商家销售的某个产品，包含商家对该产品的自定义信息
    /// 是连接商家和产品的桥梁，支持多对多关系
    /// 对应接口：GET /api/merchant/bearings、POST /api/merchant/bearings等
    /// </summary>
    public class MerchantBearing : BaseEntity
    {
        /// <summary>
        /// 商家ID
        /// </summary>
        public Guid MerchantId { get; private set; }

        /// <summary>
        /// 商家导航属性
        /// </summary>
        public Merchant? Merchant { get; private set; }

        /// <summary>
        /// 轴承ID
        /// </summary>
        public Guid BearingId { get; private set; }

        /// <summary>
        /// 轴承导航属性
        /// </summary>
        public Bearing? Bearing { get; private set; }

        /// <summary>
        /// 价格描述（如 "¥55-60"、"电议"、"面议"）
        /// 使用字符串而非数值，因为实际业务中价格表述多样
        /// </summary>
        public string? PriceDescription { get; private set; }

        /// <summary>
        /// 价格可见性
        /// Public: 所有人都可见
        /// LoginRequired: 仅登录用户可见
        /// </summary>
        public PriceVisibility PriceVisibility { get; private set; } = PriceVisibility.Public;

        /// <summary>
        /// 数值化价格（用于排序和筛选）
        /// 当PriceDescription为"电议"时可设为null
        /// </summary>
        public decimal? NumericPrice { get; private set; }

        /// <summary>
        /// 库存描述（如 "现货"、"期货"、"需预订"）
        /// </summary>
        public string? StockDescription { get; private set; }

        /// <summary>
        /// 最小起订量描述（如 "1套起订"、"量大优惠"）
        /// </summary>
        public string? MinOrderDescription { get; private set; }

        /// <summary>
        /// 商家备注
        /// </summary>
        public string? Remarks { get; private set; }

        /// <summary>
        /// 浏览次数（用于统计热门商品）
        /// </summary>
        public int ViewCount { get; private set; }

        /// <summary>
        /// 销售状态（v2.15.0 状态统一）：已下架/出售中/补货中，替代原 IsOnSale + IsRestocking 两个互斥 bool。
        /// 默认出售中（保护 互联网数据/存量数据：互联网与历史关联保持公域可见）
        /// </summary>
        public SaleState SaleState { get; private set; } = Enums.SaleState.OnSale;

        /// <summary>
        /// 补货预计到货时间（自由文本，如"一周内""10月到货"；仅补货中有意义）
        /// </summary>
        public string? RestockEta { get; private set; }

        /// <summary>
        /// 置顶到期时间（UTC，v2.3.0 商城置顶卡履约写入；null/过期=未置顶）。
        /// 型号商家列表按"未过期置顶优先"排序，是商户花积分买曝光的唯一入口
        /// </summary>
        public DateTime? PinnedUntil { get; private set; }

        /// <summary>当前是否处于置顶期（按传入 UTC now 判定，避免各处口径漂移）</summary>
        public bool IsPinned(DateTime nowUtc) => PinnedUntil.HasValue && PinnedUntil.Value > nowUtc;

        /// <summary>
        /// 置顶/续期（商城置顶卡履约调用）：未过期则从到期时间续加，已过期则从现在起算——
        /// 保证商户续买不亏时长
        /// </summary>
        public void Pin(DateTime nowUtc, int hours)
        {
            var baseline = (PinnedUntil.HasValue && PinnedUntil.Value > nowUtc) ? PinnedUntil.Value : nowUtc;
            PinnedUntil = baseline.AddHours(hours);
            UpdatedAt = nowUtc;
        }

        /// <summary>
        /// 审核状态（v2.15.0 状态统一）：仓库中/审核中/已通过/已驳回，替代原 IsPendingApproval bool。
        /// 默认已通过（保护 互联网数据/存量数据）；商家新建走 MoveToWarehouse 落仓库中
        /// </summary>
        public ListingReviewStatus ReviewStatus { get; private set; } = ListingReviewStatus.Approved;

        /// <summary>
        /// 审核意见（如果被拒绝）
        /// </summary>
        public string? ApprovalComment { get; private set; }

        /// <summary>
        /// 数据来源类型（互联网/手动录入）
        /// </summary>
        public DataSourceType? DataSourceType { get; private set; }

        /// <summary>
        /// 商家商品实拍图 URL 列表（v2.22.0 新增）：最多 3 张，首个为首图。
        /// 只属于本商家的这件商品，不写入轴承主数据图，商城与商家卡片优先展示此图。
        /// </summary>
        public string[] ImageUrls { get; private set; } = [];

        /// <summary>
        /// 无参构造函数，仅供EF Core使用
        /// </summary>
        private MerchantBearing() { }

        /// <summary>
        /// 创建商家产品关联
        /// </summary>
        /// <param name="merchantId">商家ID</param>
        /// <param name="bearingId">轴承ID</param>
        /// <param name="priceDescription">价格描述</param>
        /// <param name="stockDescription">库存描述</param>
        /// <exception cref="ArgumentException">参数验证异常</exception>
        public MerchantBearing(
            Guid merchantId,
            Guid bearingId,
            string? priceDescription = null,
            string? stockDescription = null)
        {
            if (merchantId == Guid.Empty)
                throw new ArgumentException("商家ID不能为空", nameof(merchantId));
            if (bearingId == Guid.Empty)
                throw new ArgumentException("轴承ID不能为空", nameof(bearingId));

            MerchantId = merchantId;
            BearingId = bearingId;
            PriceDescription = priceDescription;
            StockDescription = stockDescription;

            // v2.15.0 状态统一：默认出售中 + 已通过（保护 互联网数据/存量数据；商家新建走 MoveToWarehouse 落仓库中）
            SaleState = Enums.SaleState.OnSale;
            ReviewStatus = ListingReviewStatus.Approved;
        }

        /// <summary>
        /// 商家下架产品
        /// 改动说明（v1.36.0）：显式下架同时清除补货标记（下架=彻底不展示，与补货中互斥）
        /// </summary>
        /// <exception cref="InvalidOperationException">产品已下架时抛出</exception>
        public void TakeOffShelf()
        {
            if (SaleState == Enums.SaleState.OffShelf)
                throw new InvalidOperationException("产品已下架");

            SaleState = Enums.SaleState.OffShelf;
            RestockEta = null;
            UpdateTimestamp();

            // 触发产品下架事件
            AddDomainEvent(new BearingTakenOffShelfEvent(Id, MerchantId, BearingId));
        }

        /// <summary>
        /// 商家重新上架
        /// 改动说明（v1.36.0）：回到在售时清除补货态（三态互斥）
        /// </summary>
        /// <exception cref="InvalidOperationException">产品已在售时抛出</exception>
        public void PutOnShelf()
        {
            if (SaleState == Enums.SaleState.OnSale)
                throw new InvalidOperationException("产品已在售");

            SaleState = Enums.SaleState.OnSale;
            RestockEta = null;
            UpdateTimestamp();

            // 触发产品上架事件
            AddDomainEvent(new BearingPutOnShelfEvent(Id, MerchantId, BearingId));
        }

        /// <summary>
        /// 置为补货中（v1.36.0 三态）：有该型号但暂时缺货，商家列表仍展示带徽标；
        /// 在售与补货中互斥，从在售转入时同步下架
        /// </summary>
        /// <param name="restockEta">预计到货时间（自由文本，可空）</param>
        public void MarkAsRestocking(string? restockEta)
        {
            SaleState = Enums.SaleState.Restocking;
            RestockEta = string.IsNullOrWhiteSpace(restockEta) ? null : restockEta.Trim();
            UpdateTimestamp();
        }

        /// <summary>
        /// 落入仓库中（v2.15.0 状态统一）：商家新建商品的初始态——审核位 None、销售态已下架，仅商家后台可见
        /// </summary>
        public void MoveToWarehouse()
        {
            ReviewStatus = ListingReviewStatus.None;
            SaleState = Enums.SaleState.OffShelf;
            ApprovalComment = null;
            UpdateTimestamp();
        }

        /// <summary>
        /// 提交审核（商家上架或编辑重要字段后）
        /// </summary>
        public void SubmitForApproval()
        {
            ReviewStatus = ListingReviewStatus.Pending;
            ApprovalComment = null;
            UpdateTimestamp();

            AddDomainEvent(new BearingSubmittedForApprovalEvent(Id, MerchantId, BearingId));
        }

        /// <summary>
        /// 审核通过（管理员调用）：审核位 Approved + 销售态出售中（通过即上架）
        /// </summary>
        public void Approve()
        {
            ReviewStatus = ListingReviewStatus.Approved;
            SaleState = Enums.SaleState.OnSale;
            ApprovalComment = null;
            UpdateTimestamp();

            AddDomainEvent(new BearingApprovedEvent(Id, MerchantId, BearingId));
        }

        /// <summary>
        /// 审核拒绝（管理员调用）：审核位 Rejected + 销售态已下架（公域隐藏），保留理由供商家修改重提
        /// </summary>
        /// <param name="comment">拒绝理由</param>
        /// <exception cref="ArgumentException">拒绝理由为空时抛出</exception>
        public void Reject(string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
                throw new ArgumentException("拒绝理由不能为空", nameof(comment));

            ReviewStatus = ListingReviewStatus.Rejected;
            SaleState = Enums.SaleState.OffShelf;
            ApprovalComment = comment;
            UpdateTimestamp();

            AddDomainEvent(new BearingRejectedEvent(Id, MerchantId, BearingId, comment));
        }

        /// <summary>
        /// 更新市场信息
        /// </summary>
        /// <param name="priceDescription">价格描述</param>
        /// <param name="stockDescription">库存描述</param>
        /// <param name="minOrderDescription">起订量描述</param>
        /// <param name="remarks">备注</param>
        public void UpdateMarketInfo(
            string? priceDescription,
            string? stockDescription,
            string? minOrderDescription,
            string? remarks)
        {
            PriceDescription = priceDescription;
            StockDescription = stockDescription;
            MinOrderDescription = minOrderDescription;
            Remarks = remarks;
            UpdateTimestamp();
        }

        /// <summary>
        /// 设置价格信息（包含可见性和数值化价格）
        /// </summary>
        /// <param name="priceDescription">价格描述</param>
        /// <param name="numericPrice">数值化价格（用于排序）</param>
        /// <param name="visibility">价格可见性</param>
        public void SetPrice(string? priceDescription, decimal? numericPrice = null, PriceVisibility visibility = PriceVisibility.Public)
        {
            PriceDescription = priceDescription;
            NumericPrice = numericPrice;
            PriceVisibility = visibility;
            UpdateTimestamp();
        }

        /// <summary>
        /// 设置价格可见性
        /// </summary>
        /// <param name="visibility">价格可见性</param>
        public void SetPriceVisibility(PriceVisibility visibility)
        {
            PriceVisibility = visibility;
            UpdateTimestamp();
        }

        /// <summary>
        /// 设置数值化价格
        /// </summary>
        /// <param name="numericPrice">数值化价格</param>
        public void SetNumericPrice(decimal? numericPrice)
        {
            NumericPrice = numericPrice;
            UpdateTimestamp();
        }

        /// <summary>
        /// 增加浏览次数
        /// </summary>
        public void IncrementViewCount()
        {
            ViewCount++;
            UpdateTimestamp();
        }

        /// <summary>
        /// 设置数据来源类型
        /// </summary>
        public void SetDataSourceType(DataSourceType type)
        {
            DataSourceType = type;
            UpdateTimestamp();
        }

        /// <summary>
        /// 商家商品图数量上限（v2.22.0）：够看清实物即可，避免图片墙与审核负担
        /// </summary>
        public const int MaxImageCount = 3;

        /// <summary>
        /// 当前首图（v2.22.0）：无商家图时为 null，由展示层回退到轴承主数据图
        /// </summary>
        public string? CoverImageUrl => ImageUrls.Length > 0 ? ImageUrls[0] : null;

        /// <summary>
        /// 追加一张商家商品实拍图（v2.22.0）：地址为空或已达上限时抛业务异常
        /// </summary>
        /// <param name="url">图片访问地址（对象存储返回）</param>
        public void AddImage(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException("图片地址不能为空");
            if (ImageUrls.Length >= MaxImageCount)
                throw new InvalidOperationException($"最多只能上传 {MaxImageCount} 张商品图片");

            ImageUrls = [.. ImageUrls, url.Trim()];
            UpdateTimestamp();
        }

        /// <summary>
        /// 按地址移除一张商家商品实拍图（v2.22.0）：地址不在列表中时抛业务异常
        /// </summary>
        /// <param name="url">要移除的图片地址</param>
        public void RemoveImage(string url)
        {
            var target = (url ?? string.Empty).Trim();
            if (!ImageUrls.Contains(target))
                throw new InvalidOperationException("图片不存在或已被删除");

            ImageUrls = ImageUrls.Where(u => u != target).ToArray();
            UpdateTimestamp();
        }

        /// <summary>
        /// 重排商家商品图（v2.22.0）：首项即首图；传入集合必须与现有图片完全一致，
        /// 防止前端用旧列表覆盖掉后上传的图片
        /// </summary>
        /// <param name="orderedUrls">按目标顺序排列的图片地址</param>
        public void SetImageOrder(IReadOnlyList<string> orderedUrls)
        {
            var incoming = orderedUrls?.Select(u => (u ?? string.Empty).Trim()).ToList() ?? [];
            if (incoming.Count != ImageUrls.Length || incoming.Distinct().Count() != incoming.Count ||
                !incoming.All(ImageUrls.Contains))
            {
                throw new InvalidOperationException("图片列表已变化，请刷新后重试");
            }

            ImageUrls = [.. incoming];
            UpdateTimestamp();
        }

        /// <summary>
        /// 公域可见判定（v2.15.0 状态统一）：审核通过 + 未删除 + 销售态为出售中/补货中。
        /// 商城 feed、轴承商家列表、店铺页三面公域查询统一用此口径
        /// </summary>
        public bool IsPubliclyVisible =>
            IsActive && ReviewStatus == ListingReviewStatus.Approved &&
            (SaleState == Enums.SaleState.OnSale || SaleState == Enums.SaleState.Restocking);

        /// <summary>
        /// 商家删除守卫（v2.15.0 商家删除）：公域可见态（出售中/补货中）不可直接删除，须先下架；
        /// 其余态（仓库中/审核中/已驳回/已下架）均可直接删除（物理删除，无软删残留）
        /// </summary>
        /// <exception cref="InvalidOperationException">当前在售/补货中时抛出</exception>
        public void EnsureRemovable()
        {
            if (SaleState == Enums.SaleState.OnSale || SaleState == Enums.SaleState.Restocking)
                throw new InvalidOperationException("出售中/补货中的商品请先下架再删除");
        }

        /// <summary>
        /// 判断是否有效库存（有现货）
        /// </summary>
        public bool HasStock() =>
            !string.IsNullOrWhiteSpace(StockDescription) &&
            StockDescription.Contains("现货", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// 是否有议价空间（电议、面议）
        /// </summary>
        public bool IsNegotiable() =>
            !string.IsNullOrWhiteSpace(PriceDescription) &&
            (PriceDescription.Contains("电议", StringComparison.OrdinalIgnoreCase) ||
             PriceDescription.Contains("面议", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// 判断价格是否对当前用户可见
        /// </summary>
        /// <param name="isAuthenticated">用户是否已登录</param>
        /// <returns>是否可见</returns>
        public bool IsPriceVisible(bool isAuthenticated) =>
            PriceVisibility == PriceVisibility.Public ||
            (PriceVisibility == PriceVisibility.LoginRequired && isAuthenticated);

    }
}
