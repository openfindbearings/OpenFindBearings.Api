using MediatR;

namespace OpenFindBearings.Application.Features.Roles.Commands
{
    /// <summary>
    /// 分配权限给角色命令
    /// </summary>
    public record AssignPermissionsToRoleCommand : IRequest
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
