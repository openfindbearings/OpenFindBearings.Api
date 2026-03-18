using MediatR;

namespace OpenFindBearings.Application.Features.Permissions.Commands
{
    /// <summary>
    /// 删除权限命令
    /// </summary>
    public record DeletePermissionCommand(Guid Id) : IRequest;
}
