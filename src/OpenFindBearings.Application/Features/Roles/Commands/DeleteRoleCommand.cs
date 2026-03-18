using MediatR;

namespace OpenFindBearings.Application.Features.Roles.Commands
{
    /// <summary>
    /// 删除角色命令
    /// </summary>
    public record DeleteRoleCommand(Guid Id) : IRequest;
}
