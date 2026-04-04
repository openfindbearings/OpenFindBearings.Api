namespace OpenFindBearings.Application.Features.Bearings.DTOs
{
    /// <summary>
    /// 轴承列表项DTO
    /// </summary>
    public class BearingDto
    {
        public Guid Id { get; set; }

        /// <summary>
        /// 现行代号
        /// </summary>
        public string CurrentCode { get; set; } = string.Empty;

        /// <summary>
        /// 曾用代号
        /// </summary>
        public string? FormerCode { get; set; }

        /// <summary>
        /// 产品名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 产品描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 轴承类型名称
        /// </summary>
        public string BearingType { get; set; } = string.Empty;

        // 尺寸参数
        public decimal InnerDiameter { get; set; }
        public decimal OuterDiameter { get; set; }
        public decimal Width { get; set; }
        public decimal? Weight { get; set; }

        // 品牌信息
        public Guid BrandId { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public string? BrandCountry { get; set; }

        // 产地和类别信息
        public string? OriginCountry { get; set; }
        public string Category { get; set; } = string.Empty;

        // 类型信息
        public Guid BearingTypeId { get; set; }
        public string BearingTypeName { get; set; } = string.Empty;

        // 是否标准
        public bool IsStandard { get; set; }

        // 统计
        public int ViewCount { get; set; }
        public int FavoriteCount { get; set; }
    }
}
