using MediatR;

namespace OpenFindBearings.Application.Features.Roles.Commands
{
    /// <summary>
    /// 创建角色命令
    /// </summary>
    public record CreateRoleCommand : IRequest<Guid>
    {
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
