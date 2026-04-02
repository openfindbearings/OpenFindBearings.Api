using MediatR;
using OpenFindBearings.Application.Features.Sync.DTOs;

namespace OpenFindBearings.Application.Features.Sync.Commands
{
    public record BatchCreateInterchangesCommand : IRequest<BatchResult>
    {
        public List<SyncInterchangeDto> Interchanges { get; init; } = new();
    }
}
