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
    }
}
