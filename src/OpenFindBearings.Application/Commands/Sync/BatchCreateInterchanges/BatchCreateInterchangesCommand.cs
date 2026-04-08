using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Commands.Sync.BatchCreateInterchanges
{
    public record BatchCreateInterchangesCommand : IRequest<BatchResult>, ICommand
    {
        public List<SyncInterchangeDto> Interchanges { get; init; } = [];
    }
}
