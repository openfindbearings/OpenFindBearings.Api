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
    }
}
