using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.BearingTypes.HardDeleteBearingType
{
    /// <summary>
    /// 彻底删除轴承类型命令（物理删除，仅限已软删除的记录）
    /// </summary>
    public record HardDeleteBearingTypeCommand(Guid Id) : IRequest, ICommand;
}
