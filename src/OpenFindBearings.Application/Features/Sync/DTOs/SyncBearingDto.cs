using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.Features.Sync.DTOs
{
    /// <summary>
    /// 同步轴承DTO（用于爬虫数据导入）
    /// </summary>
    public class SyncBearingDto
    {
        /// <summary>
        /// 轴承型号
        /// </summary>
        public string PartNumber { get; set; } = string.Empty;

        /// <summary>
        /// 产品名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 产品描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 内径 (mm)
        /// </summary>
        public decimal InnerDiameter { get; set; }

        /// <summary>
        /// 外径 (mm)
        /// </summary>
        public decimal OuterDiameter { get; set; }

        /// <summary>
        /// 宽度 (mm)
        /// </summary>
        public decimal Width { get; set; }

        /// <summary>
        /// 重量 (kg)
        /// </summary>
        public decimal? Weight { get; set; }

        /// <summary>
        /// 精度等级
        /// </summary>
        public string? PrecisionGrade { get; set; }

        /// <summary>
        /// 材料
        /// </summary>
        public string? Material { get; set; }

        /// <summary>
        /// 密封方式
        /// </summary>
        public string? SealType { get; set; }

        /// <summary>
        /// 保持架类型
        /// </summary>
        public string? CageType { get; set; }

        /// <summary>
        /// 动载荷 (kN)
        /// </summary>
        public decimal? DynamicLoadRating { get; set; }

        /// <summary>
        /// 静载荷 (kN)
        /// </summary>
        public decimal? StaticLoadRating { get; set; }

        /// <summary>
        /// 极限转速 (rpm)
        /// </summary>
        public decimal? LimitingSpeed { get; set; }

        /// <summary>
        /// 品牌代码（如 SKF、FAG）
        /// </summary>
        public string BrandCode { get; set; } = string.Empty;

        /// <summary>
        /// 轴承类型代码（如 DGBB、ACBB）
        /// </summary>
        public string BearingTypeCode { get; set; } = string.Empty;

        /// <summary>
        /// 产地（原产国/地区）
        /// 如：瑞典、德国、日本、中国
        /// </summary>
        public string? OriginCountry { get; set; }

        /// <summary>
        /// 产品类别
        /// 1: 进口, 2: 国产, 3: 合资, 4: 其他
        /// </summary>
        public ProductCategory Category { get; set; } = ProductCategory.Domestic;
    }
}
