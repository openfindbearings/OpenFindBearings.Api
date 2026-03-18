using MediatR;

namespace OpenFindBearings.Application.Features.Bearings.Commands
{
    /// <summary>
    /// 创建轴承命令
    /// </summary>
    public record CreateBearingCommand : IRequest<Guid>
    {
        /// <summary>
        /// 轴承型号
        /// </summary>
        public string PartNumber { get; init; } = string.Empty;

        /// <summary>
        /// 产品名称
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// 产品描述
        /// </summary>
        public string? Description { get; init; }

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
    }
}
