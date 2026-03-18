using MediatR;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.Features.Brands.Commands
{
    /// <summary>
    /// 创建品牌命令
    /// </summary>
    public record CreateBrandCommand : IRequest<Guid>
    {
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Country { get; init; }
        public string? LogoUrl { get; init; }
        public BrandLevel Level { get; init; }
    }
}
