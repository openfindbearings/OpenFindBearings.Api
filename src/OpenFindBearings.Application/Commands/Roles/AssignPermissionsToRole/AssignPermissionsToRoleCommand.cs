using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Roles.AssignPermissionsToRole
{
    /// <summary>
    /// 分配权限给角色命令
    /// </summary>
    public record AssignPermissionsToRoleCommand : IRequest, ICommand
    {
        /// <summary>
        /// 角色ID
        /// </summary>
        public Guid RoleId { get; init; }

        /// <summary>
        /// 权限名称列表
        /// </summary>
        public List<string> PermissionNames { get; init; } = new();
    }
}
