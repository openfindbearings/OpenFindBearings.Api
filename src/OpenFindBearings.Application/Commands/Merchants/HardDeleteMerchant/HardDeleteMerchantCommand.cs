using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Merchants.HardDeleteMerchant
{
    /// <summary>
    /// 彻底删除商家命令（物理删除，仅限已软删除的记录）
    /// </summary>
    public record HardDeleteMerchantCommand(Guid Id) : IRequest, ICommand;
}
