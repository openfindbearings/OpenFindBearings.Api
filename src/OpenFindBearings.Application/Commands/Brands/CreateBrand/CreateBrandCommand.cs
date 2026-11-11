using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.Commands.Brands.CreateBrand
{
    /// <summary>
    /// 创建品牌命令
    /// </summary>
    public record CreateBrandCommand : IRequest<Guid>, ICommand
    {
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Country { get; init; }
        public string? LogoUrl { get; init; }
        public BrandLevel Level { get; init; }

        // 改动说明（品牌资料完善）：新建时可直接录入资料字段，全部可空
        public string? Description { get; init; }
        public int? FoundedYear { get; init; }
        public string? ParentCompany { get; init; }
        public string? Website { get; init; }
        public string? Advantages { get; init; }
        public string? SuffixNotes { get; init; }
        public string? MainApplications { get; init; }
    }
}
