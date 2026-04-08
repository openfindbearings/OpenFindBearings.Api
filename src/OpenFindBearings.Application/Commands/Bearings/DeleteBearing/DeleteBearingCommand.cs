using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Bearings.DeleteBearing
{
    /// <summary>
    /// 删除轴承命令
    /// </summary>
    public record DeleteBearingCommand(Guid Id) : IRequest, ICommand;
}
