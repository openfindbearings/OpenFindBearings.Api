using MediatR;
using OpenFindBearings.Application.Features.Roles.DTOs;

namespace OpenFindBearings.Application.Features.Roles.Queries
{
    /// <summary>
    /// 获取单个角色查询
    /// </summary>
    public record GetRoleQuery(Guid Id) : IRequest<RoleDto?>;
}
