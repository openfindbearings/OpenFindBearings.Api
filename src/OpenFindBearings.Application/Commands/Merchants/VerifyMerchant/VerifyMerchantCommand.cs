using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Merchants.VerifyMerchant
{
    /// <summary>
    /// 认证商家命令
    /// </summary>
    public record VerifyMerchantCommand(Guid Id) : IRequest, ICommand;
}
