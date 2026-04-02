using MediatR;
using OpenFindBearings.Application.Features.Sync.DTOs;

namespace OpenFindBearings.Application.Features.Sync.Commands
{
    public record BatchCreateMerchantBearingsCommand : IRequest<BatchResult>
    {
        public List<SyncMerchantBearingDto> MerchantBearings { get; init; } = new();
    }
}
