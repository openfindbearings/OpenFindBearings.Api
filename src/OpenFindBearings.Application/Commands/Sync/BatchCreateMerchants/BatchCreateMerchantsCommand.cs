using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Commands.Sync.BatchCreateMerchants
{
    public record BatchCreateMerchantsCommand : IRequest<BatchResult>, ICommand
    {
        public List<SyncMerchantDto> Merchants { get; init; } = [];
        public SyncMode Mode { get; init; } = SyncMode.Upsert;
    }
}
