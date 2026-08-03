using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Merchants.RejectMerchant
{
    public record RejectMerchantCommand(Guid Id, string Reason) : IRequest, ICommand;
}
