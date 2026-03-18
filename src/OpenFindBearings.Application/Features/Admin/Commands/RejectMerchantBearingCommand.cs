using MediatR;

namespace OpenFindBearings.Application.Features.Admin.Commands
{
    public record RejectMerchantBearingCommand(Guid MerchantBearingId, string Reason) : IRequest;
}
