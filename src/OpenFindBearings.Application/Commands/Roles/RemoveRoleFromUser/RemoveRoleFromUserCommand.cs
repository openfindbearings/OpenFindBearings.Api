using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Roles.RemoveRoleFromUser
{
    /// <summary>
    /// 从用户移除角色命令
    /// </summary>
    public record RemoveRoleFromUserCommand : IRequest, ICommand
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
