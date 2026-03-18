using MediatR;
using OpenFindBearings.Application.Common.Enums;
using OpenFindBearings.Application.Common.Models;
using OpenFindBearings.Application.Features.Sync.DTOs;

namespace OpenFindBearings.Application.Features.Sync.Commands
{
    public record BatchCreateMerchantsCommand : IRequest<BatchResult>
    {
        public List<SyncMerchantDto> Merchants { get; init; } = new();
        public SyncMode Mode { get; init; } = SyncMode.Upsert;
    }
}
