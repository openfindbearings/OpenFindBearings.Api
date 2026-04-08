using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Roles.Commands
{
    /// <summary>
    /// 创建角色命令
    /// </summary>
    public record CreateRoleCommand : IRequest<Guid>, ICommand
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
