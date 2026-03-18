namespace OpenFindBearings.Application.Features.Brands.DTOs
{
    /// <summary>
    /// 品牌DTO
    /// </summary>
    public class BrandDto
    {
        /// <summary>
        /// 品牌ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 品牌代码（如 SKF）
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 品牌名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 所属国家
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// 品牌Logo
        /// </summary>
        public string? LogoUrl { get; set; }

        /// <summary>
        /// 品牌档次
        /// </summary>
        public string Level { get; set; } = string.Empty;

        /// <summary>
        /// 轴承数量统计
        /// </summary>
        public int BearingCount { get; set; }
    }
}
