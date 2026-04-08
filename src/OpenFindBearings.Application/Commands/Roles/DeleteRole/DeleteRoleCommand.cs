using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Roles.DeleteRole
{
    /// <summary>
    /// 删除角色命令
    /// </summary>
    public record DeleteRoleCommand(Guid Id) : IRequest, ICommand;
}
