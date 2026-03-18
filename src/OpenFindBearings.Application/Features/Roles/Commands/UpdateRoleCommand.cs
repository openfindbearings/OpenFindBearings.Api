using MediatR;

namespace OpenFindBearings.Application.Features.Roles.Commands
{
    /// <summary>
    /// 更新角色命令
    /// </summary>
    public record UpdateRoleCommand : IRequest
    {
        /// <summary>
        /// 角色ID
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// 角色名称
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// 角色描述
        /// </summary>
        public string? Description { get; init; }
    }
}
