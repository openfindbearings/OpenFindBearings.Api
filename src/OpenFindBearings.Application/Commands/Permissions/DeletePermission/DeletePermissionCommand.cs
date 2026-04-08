using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Permissions.DeletePermission
{
    /// <summary>
    /// 删除权限命令
    /// </summary>
    public record DeletePermissionCommand(Guid Id) : IRequest, ICommand;
}
