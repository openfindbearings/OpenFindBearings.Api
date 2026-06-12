using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Bearings.HardDeleteBearing
{
    /// <summary>
    /// 彻底删除轴承命令（物理删除，仅限已软删除的记录）
    /// </summary>
    public record HardDeleteBearingCommand(Guid Id) : IRequest, ICommand;
}
