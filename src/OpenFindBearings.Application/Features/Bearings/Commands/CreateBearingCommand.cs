using MediatR;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.Features.Bearings.Commands
{
    /// <summary>
    /// 创建轴承命令
    /// </summary>
    public record CreateBearingCommand : IRequest<Guid>
    {
        /// <summary>
        /// 轴承型号（现行代号）
        /// </summary>
        public string CurrentCode { get; init; } = string.Empty;

        /// <summary>
        /// 曾用代号
        /// </summary>
        public string? FormerCode { get; init; }

        /// <summary>
        /// 代号来源
        /// </summary>
        public string? CodeSource { get; init; }

        /// <summary>
        /// 产品名称
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// 产品描述
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// 轴承类型（名称）
        /// </summary>
        public string BearingType { get; init; } = string.Empty;

        /// <summary>
        /// 结构类型
        /// </summary>
        public string? StructureType { get; init; }

        /// <summary>
        /// 尺寸系列
        /// </summary>
        public string? SizeSeries { get; init; }

        /// <summary>
        /// 是否为标准轴承
        /// </summary>
        public bool IsStandard { get; init; } = true;

        /// <summary>
        /// 内径 (mm)
        /// </summary>
        public decimal InnerDiameter { get; init; }

        /// <summary>
        /// 外径 (mm)
        /// </summary>
        public decimal OuterDiameter { get; init; }

        /// <summary>
        /// 宽度 (mm)
        /// </summary>
        public decimal Width { get; init; }

        /// <summary>
        /// 最小倒角 (mm)
        /// </summary>
        public decimal? ChamferRmin { get; init; }

        /// <summary>
        /// 最大倒角 (mm)
        /// </summary>
        public decimal? ChamferRmax { get; init; }

        /// <summary>
        /// 重量 (kg)
        /// </summary>
        public decimal? Weight { get; init; }

        /// <summary>
        /// 精度等级
        /// </summary>
        public string? PrecisionGrade { get; init; }

        /// <summary>
        /// 材料
        /// </summary>
        public string? Material { get; init; }

        /// <summary>
        /// 密封方式
        /// </summary>
        public string? SealType { get; init; }

        /// <summary>
        /// 保持架类型
        /// </summary>
        public string? CageType { get; init; }

        /// <summary>
        /// 动载荷 (kN)
        /// </summary>
        public decimal? DynamicLoadRating { get; init; }

        /// <summary>
        /// 静载荷 (kN)
        /// </summary>
        public decimal? StaticLoadRating { get; init; }

        /// <summary>
        /// 极限转速 (rpm)
        /// </summary>
        public decimal? LimitingSpeed { get; init; }

        /// <summary>
        /// 轴承类型ID
        /// </summary>
        public Guid BearingTypeId { get; init; }

        /// <summary>
        /// 品牌ID
        /// </summary>
        public Guid BrandId { get; init; }

        /// <summary>
        /// 商标
        /// </summary>
        public string? Trademark { get; init; }

        /// <summary>
        /// 产地（原产国/地区）
        /// </summary>
        public string? OriginCountry { get; init; }

        /// <summary>
        /// 产品类别
        /// </summary>
        public BearingCategory Category { get; init; } = BearingCategory.Domestic;
    }
}
