using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Bearings.RestoreBearing
{
    /// <summary>
    /// 恢复已删除轴承命令
    /// </summary>
    public record RestoreBearingCommand(Guid Id) : IRequest, ICommand;
}
