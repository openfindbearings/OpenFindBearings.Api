using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Merchants.RestoreMerchant
{
    /// <summary>
    /// 恢复已删除商家命令
    /// </summary>
    public record RestoreMerchantCommand(Guid Id) : IRequest, ICommand;
}
