using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.BearingTypes.DeleteBearingType
{
    /// <summary>
    /// 删除轴承类型命令（软删除）
    /// </summary>
    public record DeleteBearingTypeCommand(Guid Id) : IRequest, ICommand;
}
