namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 同步商家-轴承关联DTO
    /// </summary>
    public class SyncMerchantBearingDto
    {
        /// <summary>
        /// 商家名称（用于匹配）
        /// </summary>
        public string MerchantName { get; set; } = string.Empty;

        /// <summary>
        /// 轴承型号（用于匹配）
        /// </summary>
        public string BearingPartNumber { get; set; } = string.Empty;

        /// <summary>
        /// 品牌代码（用于精确匹配）
        /// </summary>
        public string? BrandCode { get; set; }

        /// <summary>
        /// 价格描述
        /// </summary>
        public string? PriceDescription { get; set; }

        /// <summary>
        /// 库存描述
        /// </summary>
        public string? StockDescription { get; set; }

        /// <summary>
        /// 最小起订量描述
        /// </summary>
        public string? MinOrderDescription { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remarks { get; set; }

        /// <summary>
        /// 是否在售
        /// </summary>
        public bool IsOnSale { get; set; } = true;
    }
}
