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
        /// 是否推荐/置顶
        /// 用于商家自定义推荐产品
        /// </summary>
        public bool IsFeatured { get; private set; }

        /// <summary>
        /// 是否在售（商家控制）
        /// true: 在售（默认）
        /// false: 已下架
        /// </summary>
        public bool IsOnSale { get; private set; } = true;

        /// <summary>
        /// 是否需要审核（用于商家创建/编辑产品）
        /// true: 待审核
        /// false: 已审核通过
        /// </summary>
        public bool IsPendingApproval { get; private set; }

        /// <summary>
        /// 审核意见（如果被拒绝）
        /// </summary>
        public string? ApprovalComment { get; private set; }

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

            IsOnSale = true;
            IsPendingApproval = false;
        }

        /// <summary>
        /// 商家下架产品
        /// </summary>
        /// <exception cref="InvalidOperationException">产品已下架时抛出</exception>
        public void TakeOffShelf()
        {
            if (!IsOnSale)
                throw new InvalidOperationException("产品已下架");

            IsOnSale = false;
            UpdateTimestamp();

            // 触发产品下架事件
            AddDomainEvent(new BearingTakenOffShelfEvent(Id, MerchantId, BearingId));
        }

        /// <summary>
        /// 商家重新上架
        /// </summary>
        /// <exception cref="InvalidOperationException">产品已在售时抛出</exception>
        public void PutOnShelf()
        {
            if (IsOnSale)
                throw new InvalidOperationException("产品已在售");

            IsOnSale = true;
            UpdateTimestamp();

            // 触发产品上架事件
            AddDomainEvent(new BearingPutOnShelfEvent(Id, MerchantId, BearingId));
        }

        /// <summary>
        /// 提交审核（商家创建或编辑后）
        /// </summary>
        public void SubmitForApproval()
        {
            IsPendingApproval = true;
            ApprovalComment = null;
            UpdateTimestamp();

            AddDomainEvent(new BearingSubmittedForApprovalEvent(Id, MerchantId, BearingId));
        }

        /// <summary>
        /// 审核通过（管理员调用）
        /// </summary>
        public void Approve()
        {
            IsPendingApproval = false;
            IsOnSale = true;
            UpdateTimestamp();

            AddDomainEvent(new BearingApprovedEvent(Id, MerchantId, BearingId));
        }

        /// <summary>
        /// 审核拒绝（管理员调用）
        /// </summary>
        /// <param name="comment">拒绝理由</param>
        /// <exception cref="ArgumentException">拒绝理由为空时抛出</exception>
        public void Reject(string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
                throw new ArgumentException("拒绝理由不能为空", nameof(comment));

            IsPendingApproval = false;
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
        /// 设置是否推荐
        /// </summary>
        /// <param name="featured">是否推荐</param>
        public void SetFeatured(bool featured)
        {
            IsFeatured = featured;
            UpdateTimestamp();
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
