using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Roles.GetRoleDetail
{

    /// <summary>
    /// 获取角色详情查询（包含权限列表）
    /// </summary>
    public record GetRoleDetailQuery(Guid RoleId) : IRequest<RoleDetailDto?>, IQuery;
}
