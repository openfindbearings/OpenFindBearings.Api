using MediatR;
using OpenFindBearings.Application.Features.Roles.DTOs;

namespace OpenFindBearings.Application.Features.Roles.Queries
{

    /// <summary>
    /// 获取角色详情查询（包含权限列表）
    /// </summary>
    public record GetRoleDetailQuery(Guid RoleId) : IRequest<RoleDetailDto?>;
}
