using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Merchants.RejectMerchant
{
    public record RejectMerchantCommand(Guid Id, string Reason, Guid? ReviewedBy = null) : IRequest, ICommand;
}
