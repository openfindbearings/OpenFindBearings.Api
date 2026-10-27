using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.DTOs
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
        /// 轴承参数的来源（v1.6.0 数据可信联动）：Internet/OcrExtract/SeedData=仅供参考，其余=可信。
        /// 用于商品卡"参数来源：互联网 · 仅供参考"灰标；来源指轴承参数而非商家
        /// </summary>
        public string? BearingDataSource { get; set; }

        /// <summary>
        /// 轴承ID
        /// </summary>
        public Guid BearingId { get; set; }

        // ============ 轴承信息 ============

        /// <summary>
        /// 轴承现行代号
        /// </summary>
        public string BearingPartNumber { get; set; } = string.Empty;

        /// <summary>
        /// 轴承曾用代号
        /// </summary>
        public string? BearingOldNumber { get; set; }

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

        // ============ 图片（v2.14.0 商城导购：商品卡需图，从关联轴承带出） ============

        /// <summary>
        /// 3D 效果图地址（取关联轴承的图，商品卡主图）
        /// </summary>
        public string? Image3DUrl { get; set; }

        /// <summary>
        /// 2D 效果图地址（取关联轴承的图，3D 缺失时的回退）
        /// </summary>
        public string? Image2DUrl { get; set; }

        // ============ 商家商品实拍图（v2.22.0：属于本商家的这件商品，不改主数据） ============

        /// <summary>
        /// 商家上传的商品实拍图地址列表（最多 3 张，首个为首图）
        /// </summary>
        public List<string> MerchantImageUrls { get; set; } = [];

        /// <summary>
        /// 展示用封面图：商家首图优先，无商家图时回退到轴承 3D/2D 主图；前端直接用它，不再自行拼接
        /// </summary>
        public string? CoverImageUrl { get; set; }

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
        /// 销售状态（v2.15.0 状态统一）：0=已下架 1=出售中 2=补货中
        /// </summary>
        public SaleState SaleState { get; set; }

        /// <summary>
        /// 补货预计到货时间（自由文本，仅补货中有意义）
        /// </summary>
        public string? RestockEta { get; set; }

        /// <summary>
        /// 审核状态（v2.15.0 状态统一）：0=仓库中 1=审核中 2=已通过 3=已驳回
        /// </summary>
        public ListingReviewStatus ReviewStatus { get; set; }

        /// <summary>
        /// 审核意见/驳回理由（v2.15.0：已驳回时下发，商家可见）
        /// </summary>
        public string? ApprovalComment { get; set; }

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
