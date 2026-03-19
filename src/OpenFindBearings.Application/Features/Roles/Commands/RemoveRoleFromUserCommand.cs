using MediatR;

namespace OpenFindBearings.Application.Features.Roles.Commands
{
    /// <summary>
    /// 从用户移除角色命令
    /// </summary>
    public record RemoveRoleFromUserCommand : IRequest
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
