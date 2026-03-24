namespace OpenFindBearings.Application.Features.Mobile.DTOs
{
    /// <summary>
    /// 移动端轴承轻量DTO
    /// </summary>
    public class MobileBearingLightDto
    {
        /// <summary>
        /// 轴承ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 轴承型号
        /// </summary>
        public string PartNumber { get; set; } = string.Empty;

        /// <summary>
        /// 品牌名称
        /// </summary>
        public string BrandName { get; set; } = string.Empty;

        /// <summary>
        /// 内径
        /// </summary>
        public decimal InnerDiameter { get; set; }

        /// <summary>
        /// 外径
        /// </summary>
        public decimal OuterDiameter { get; set; }

        /// <summary>
        /// 宽度
        /// </summary>
        public decimal Width { get; set; }

        /// <summary>
        /// 缩略图URL
        /// </summary>
        public string? ThumbnailUrl { get; set; }

        /// <summary>
        /// 最低价格
        /// </summary>
        public decimal? MinPrice { get; set; }

        /// <summary>
        /// 产地（原产国/地区）
        /// 如：瑞典、德国、日本、中国
        /// </summary>
        public string? OriginCountry { get; set; }

        /// <summary>
        /// 产品类别
        /// 进口/国产/合资/其他
        /// </summary>
        public string Category { get; set; } = string.Empty;
    }
}
