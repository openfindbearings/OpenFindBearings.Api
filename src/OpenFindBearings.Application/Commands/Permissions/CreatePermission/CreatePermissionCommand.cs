using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Permissions.CreatePermission
{

    /// <summary>
    /// 创建权限命令
    /// </summary>
    public record CreatePermissionCommand : IRequest<Guid>, ICommand
    {
        /// <summary>
        /// 权限名称
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// 权限描述
        /// </summary>
        public string? Description { get; init; }
    }
}
