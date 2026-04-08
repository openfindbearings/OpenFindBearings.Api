using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Merchants.DeleteMerchant
{
    /// <summary>
    /// 删除商家命令
    /// </summary>
    public record DeleteMerchantCommand(Guid Id) : IRequest, ICommand;
}
