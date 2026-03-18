using MediatR;

namespace OpenFindBearings.Application.Features.BearingTypes.Commands
{
    public record UpdateBearingTypeCommand : IRequest
    {
        public Guid Id { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }
    }
}
