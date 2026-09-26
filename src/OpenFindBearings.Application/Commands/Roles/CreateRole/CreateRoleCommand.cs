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
        /// 角色机器标识（v1.38.0 起限英文标识，参与鉴权 claims 比对，处理器正则校验）
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// 角色描述
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// 人读显示名（可中文，v1.38.0），空则界面回退显示 Name
        /// </summary>
        public string? DisplayName { get; init; }
    }
}
