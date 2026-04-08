using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.DTOs
{
    /// <summary>
    /// 同步轴承DTO（用于爬虫数据导入）
    /// </summary>
    public class SyncBearingDto
    {
        /// <summary>
        /// 现行代号
        /// </summary>
        public string CurrentCode { get; set; } = string.Empty;

        /// <summary>
        /// 曾用代号
        /// </summary>
        public string? FormerCode { get; set; }

        /// <summary>
        /// 代号来源
        /// </summary>
        public string? CodeSource { get; set; }

        /// <summary>
        /// 产品名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 产品描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 轴承类型名称（冗余）
        /// </summary>
        public string BearingTypeName { get; set; } = string.Empty;

        /// <summary>
        /// 结构类型
        /// </summary>
        public string? StructureType { get; set; }

        /// <summary>
        /// 尺寸系列
        /// </summary>
        public string? SizeSeries { get; set; }

        /// <summary>
        /// 是否为标准轴承
        /// </summary>
        public bool IsStandard { get; set; } = true;

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
        /// 最小倒角 (mm)
        /// </summary>
        public decimal? ChamferRmin { get; set; }

        /// <summary>
        /// 最大倒角 (mm)
        /// </summary>
        public decimal? ChamferRmax { get; set; }

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
        /// 商标
        /// </summary>
        public string? Trademark { get; set; }

        /// <summary>
        /// 产地（原产国/地区）
        /// </summary>
        public string? OriginCountry { get; set; }

        /// <summary>
        /// 产品类别
        /// </summary>
        public BearingCategory Category { get; set; } = BearingCategory.Domestic;
    }
}
