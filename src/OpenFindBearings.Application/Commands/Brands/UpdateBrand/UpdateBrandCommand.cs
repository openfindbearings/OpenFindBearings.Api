using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.Commands.Brands.UpdateBrand
{
    public record UpdateBrandCommand : IRequest, ICommand
    {
        public Guid Id { get; init; }
        public string? Name { get; init; }
        public string? Country { get; init; }
        public string? LogoUrl { get; init; }
        public BrandLevel? Level { get; init; }

        // 改动说明（品牌资料完善）：补充可在 Admin 维护的资料字段，全部可空
        public string? Description { get; init; }
        public int? FoundedYear { get; init; }
        public string? ParentCompany { get; init; }
        public string? Website { get; init; }
        public string? Advantages { get; init; }
        public string? SuffixNotes { get; init; }
        public string? MainApplications { get; init; }
    }
}
