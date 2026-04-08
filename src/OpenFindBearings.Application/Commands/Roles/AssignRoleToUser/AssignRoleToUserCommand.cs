using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Roles.AssignRoleToUser
{
    /// <summary>
    /// 分配角色给用户命令
    /// </summary>
    public record AssignRoleToUserCommand : IRequest, ICommand
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// 角色名称
        /// </summary>
        public string RoleName { get; init; } = string.Empty;
    }
}
