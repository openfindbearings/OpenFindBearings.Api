using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Roles.UpdateRole
{
    /// <summary>
    /// 更新角色命令
    /// </summary>
    public record UpdateRoleCommand : IRequest, ICommand
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

        /// <summary>
        /// 人读显示名（可中文，v1.38.0）。Name 为鉴权机器标识不随本命令变更
        /// </summary>
        public string? DisplayName { get; init; }
    }
}
