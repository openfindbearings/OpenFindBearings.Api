using MediatR;
using OpenFindBearings.Application.Features.Roles.DTOs;

namespace OpenFindBearings.Application.Features.Roles.Queries
{
    /// <summary>
    /// 获取所有角色列表查询
    /// </summary>
    public record GetAllRolesQuery : IRequest<List<RoleDto>>;
}
