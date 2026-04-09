using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Commands.Permissions.UpdatePermission
{
    /// <summary>
    /// 更新权限命令
    /// </summary>
    public record UpdatePermissionCommand : IRequest, ICommand
    {
        /// <summary>
        /// 权限ID
        /// </summary>
        public Guid Id { get; init; }

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
