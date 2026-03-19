using MediatR;

namespace OpenFindBearings.Application.Features.Roles.Commands
{
    /// <summary>
    /// 分配角色给用户命令
    /// </summary>
    public record AssignRoleToUserCommand : IRequest
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
