using MediatR;

namespace OpenFindBearings.Application.Features.Bearings.Commands
{
    /// <summary>
    /// 更新轴承命令
    /// </summary>
    public record UpdateBearingCommand : IRequest
    {
        public Guid Id { get; set; }
        public string? Name { get; init; }
        public string? Description { get; init; }
        public decimal? Weight { get; init; }
        public string? PrecisionGrade { get; init; }
        public string? Material { get; init; }
        public string? SealType { get; init; }
        public string? CageType { get; init; }
        public decimal? DynamicLoadRating { get; init; }
        public decimal? StaticLoadRating { get; init; }
        public decimal? LimitingSpeed { get; init; }
    }
}
