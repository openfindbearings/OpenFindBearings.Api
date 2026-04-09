using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Roles.GetAllRoles
{
    /// <summary>
    /// 获取所有角色列表查询
    /// </summary>
    public record GetAllRolesQuery : IRequest<List<RoleDto>>, IQuery;
}
