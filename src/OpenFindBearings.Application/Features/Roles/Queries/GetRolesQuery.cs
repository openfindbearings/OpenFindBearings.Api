using MediatR;
using OpenFindBearings.Application.Features.Roles.DTOs;
using OpenFindBearings.Domain.Common.Models;

namespace OpenFindBearings.Application.Features.Roles.Queries
{
    /// <summary>
    /// 获取角色列表（分页）查询
    /// </summary>
    public record GetRolesQuery : IRequest<PagedResult<RoleDto>>
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
        /// 关键词（搜索角色名称）
        /// </summary>
        public string? Keyword { get; init; }
    }
}
