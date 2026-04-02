using MediatR;
using OpenFindBearings.Application.Features.Sync.DTOs;

namespace OpenFindBearings.Application.Features.Sync.Commands
{
    public record BatchCreateMerchantsCommand : IRequest<BatchResult>
    {
        public List<SyncMerchantDto> Merchants { get; init; } = new();
        public SyncMode Mode { get; init; } = SyncMode.Upsert;
    }
}
