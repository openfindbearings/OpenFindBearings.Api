using MediatR;

namespace OpenFindBearings.Application.Features.Permissions.Queries
{
    /// <summary>
    /// 检查用户是否有指定权限
    /// </summary>
    public record CheckUserPermissionQuery : IRequest<bool>
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// 权限名称
        /// </summary>
        public string PermissionName { get; init; } = string.Empty;
    }
}
