using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.BearingTypes.UpdateBearingType
{
    public record UpdateBearingTypeCommand : IRequest, ICommand
    {
        public Guid Id { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }
    }
}
