using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.Features.MerchantBearings.DTOs
{
    /// <summary>
    /// 商家-轴承关联DTO
    /// 用于展示商家在售的轴承产品
    /// </summary>
    public class MerchantBearingDto
    {
        /// <summary>
        /// 关联ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 商家ID
        /// </summary>
        public Guid MerchantId { get; set; }

        /// <summary>
        /// 商家名称（仅登录用户可见）
        /// </summary>
        public string? MerchantName { get; set; }

        /// <summary>
        /// 商家等级（所有人可见）
        /// </summary>
        public string? MerchantGrade { get; set; }

        /// <summary>
        /// 商家是否已认证（所有人可见）
        /// </summary>
        public bool MerchantIsVerified { get; set; }

        /// <summary>
        /// 商家所在城市（所有人可见，从地址提取）
        /// </summary>
        public string? MerchantCity { get; set; }

        /// <summary>
        /// 商家电话（仅登录用户可见）
        /// </summary>
        public string? MerchantPhone { get; set; }

        /// <summary>
        /// 商家地址（仅登录用户可见）
        /// </summary>
        public string? MerchantAddress { get; set; }

        /// <summary>
        /// 轴承ID
        /// </summary>
        public Guid BearingId { get; set; }

        // ============ 轴承信息 ============

        /// <summary>
        /// 轴承现行代号
        /// </summary>
        public string BearingCurrentCode { get; set; } = string.Empty;

        /// <summary>
        /// 轴承曾用代号
        /// </summary>
        public string? BearingFormerCode { get; set; }

        /// <summary>
        /// 轴承名称
        /// </summary>
        public string BearingName { get; set; } = string.Empty;

        /// <summary>
        /// 轴承类型名称
        /// </summary>
        public string? BearingTypeName { get; set; }

        // ============ 品牌信息 ============

        /// <summary>
        /// 品牌名称
        /// </summary>
        public string? BrandName { get; set; }

        /// <summary>
        /// 品牌档次
        /// </summary>
        public string? BrandLevel { get; set; }

        // ============ 尺寸信息 ============

        /// <summary>
        /// 尺寸描述（如 "25×52×15"）
        /// </summary>
        public string? Dimensions { get; set; }

        // ============ 价格相关 ============

        /// <summary>
        /// 价格描述（如 "¥55-60"、"电议"）
        /// </summary>
        public string? PriceDescription { get; set; }

        /// <summary>
        /// 价格可见性
        /// </summary>
        public PriceVisibility PriceVisibility { get; set; } = PriceVisibility.Public;

        /// <summary>
        /// 是否对当前用户可见
        /// </summary>
        public bool IsPriceVisible { get; set; }

        /// <summary>
        /// 数值化价格
        /// </summary>
        public decimal? NumericPrice { get; set; }

        // ============ 其他业务信息 ============

        /// <summary>
        /// 库存描述（如 "现货"、"期货"）
        /// </summary>
        public string? StockDescription { get; set; }

        /// <summary>
        /// 最小起订量描述
        /// </summary>
        public string? MinOrderDescription { get; set; }

        /// <summary>
        /// 商家备注
        /// </summary>
        public string? Remarks { get; set; }

        /// <summary>
        /// 是否在售
        /// </summary>
        public bool IsOnSale { get; set; }

        /// <summary>
        /// 是否推荐/置顶
        /// </summary>
        public bool IsFeatured { get; set; }

        /// <summary>
        /// 是否待审核
        /// </summary>
        public bool IsPendingApproval { get; set; }

        /// <summary>
        /// 浏览次数
        /// </summary>
        public int ViewCount { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        // ============ 辅助属性 ============

        /// <summary>
        /// 判断是否为议价商品
        /// </summary>
        public bool IsNegotiable =>
            !string.IsNullOrWhiteSpace(PriceDescription) &&
            (PriceDescription.Contains("电议", StringComparison.OrdinalIgnoreCase) ||
             PriceDescription.Contains("面议", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// 判断是否有现货
        /// </summary>
        public bool HasStock =>
            !string.IsNullOrWhiteSpace(StockDescription) &&
            StockDescription.Contains("现货", StringComparison.OrdinalIgnoreCase);
    }
}
