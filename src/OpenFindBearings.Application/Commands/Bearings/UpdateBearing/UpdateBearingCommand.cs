using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.Commands.Bearings.UpdateBearing
{
    /// <summary>
    /// 更新轴承命令
    /// </summary>
    public record UpdateBearingCommand : IRequest, ICommand
    {
        public Guid Id { get; init; }

        /// <summary>
        /// 产品名称
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// 产品描述
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// 结构类型
        /// </summary>
        public string? StructureType { get; init; }

        /// <summary>
        /// 尺寸系列
        /// </summary>
        public string? SizeSeries { get; init; }

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
        /// 商标
        /// </summary>
        public string? Trademark { get; init; }

        /// <summary>
        /// 产地
        /// </summary>
        public string? OriginCountry { get; init; }

        /// <summary>
        /// 产品类别
        /// </summary>
        public BearingCategory? Category { get; init; }
    }
}
