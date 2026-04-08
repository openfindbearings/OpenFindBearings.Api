using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Application.Queries.Permissions.GetPermissions
{
    /// <summary>
    /// 获取权限列表（分页）查询
    /// </summary>
    public record GetPermissionsQuery : IRequest<PagedResult<PermissionDto>>, IQuery
    {
        /// <summary>
        /// 页码
        /// </summary>
        public int Page { get; init; } = 1;

        /// <summary>
        /// 每页条数
        /// </summary>
        public int PageSize { get; init; } = 20;

        /// <summary>
        /// 权限分组（如 product、merchant、user）
        /// </summary>
        public string? Group { get; init; }
    }
}
