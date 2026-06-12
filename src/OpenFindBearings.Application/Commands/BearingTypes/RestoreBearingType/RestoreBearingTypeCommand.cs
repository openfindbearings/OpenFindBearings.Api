using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.BearingTypes.RestoreBearingType
{
    /// <summary>
    /// 恢复已删除轴承类型命令
    /// </summary>
    public record RestoreBearingTypeCommand(Guid Id) : IRequest, ICommand;
}
