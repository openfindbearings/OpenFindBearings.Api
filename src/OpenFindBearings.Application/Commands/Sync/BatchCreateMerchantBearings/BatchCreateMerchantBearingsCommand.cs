using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Commands.Sync.BatchCreateMerchantBearings
{
    public record BatchCreateMerchantBearingsCommand : IRequest<BatchResult>, ICommand
    {
        public List<SyncMerchantBearingDto> MerchantBearings { get; init; } = [];
    }
}
