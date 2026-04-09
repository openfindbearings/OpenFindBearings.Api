using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Queries
{
    /// <summary>
    /// 获取单个角色查询
    /// </summary>
    public record GetRoleQuery(Guid Id) : IRequest<RoleDto?>, IQuery;
}
