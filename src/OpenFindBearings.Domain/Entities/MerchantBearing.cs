using OpenFindBearings.Domain.Common;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// 商家轴承关联实体
    /// 表示某个商家销售的某个产品，包含商家对该产品的自定义信息
    /// 是连接商家和产品的桥梁，支持多对多关系
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
        /// 产品ID（目前关联Bearing，未来可扩展）
        /// </summary>
        public Guid BearingId { get; private set; }

        /// <summary>
        /// 产品导航属性
        /// </summary>
        public Bearing? Bearing { get; private set; }

        /// <summary>
        /// 价格描述（如 "¥55-60"、"电议"、"面议"）
        /// 使用字符串而非数值，因为实际业务中价格表述多样
        /// </summary>
        public string? PriceDescription { get; private set; }

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
        /// 无参构造函数，仅供EF Core使用
        /// </summary>
        private MerchantBearing() { }

        /// <summary>
        /// 创建商家产品关联
        /// </summary>
        /// <param name="merchantId">商家ID</param>
        /// <param name="bearingId">产品ID</param>
        /// <param name="priceDescription">价格描述</param>
        /// <param name="stockDescription">库存描述</param>
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
    }
}
