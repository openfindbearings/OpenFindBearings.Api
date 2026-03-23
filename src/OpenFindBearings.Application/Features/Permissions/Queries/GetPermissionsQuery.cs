using MediatR;
using OpenFindBearings.Application.Features.Permissions.DTOs;
using OpenFindBearings.Domain.Common.Models;

namespace OpenFindBearings.Application.Features.Permissions.Queries
{
    /// <summary>
    /// 获取权限列表（分页）查询
    /// </summary>
    public record GetPermissionsQuery : IRequest<PagedResult<PermissionDto>>
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
