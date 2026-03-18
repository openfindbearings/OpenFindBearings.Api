using MediatR;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Application.Features.Brands.Commands
{
    public record UpdateBrandCommand : IRequest
    {
        public Guid Id { get; init; }
        public string? Name { get; init; }
        public string? Country { get; init; }
        public string? LogoUrl { get; init; }
        public BrandLevel? Level { get; init; }
    }
}
