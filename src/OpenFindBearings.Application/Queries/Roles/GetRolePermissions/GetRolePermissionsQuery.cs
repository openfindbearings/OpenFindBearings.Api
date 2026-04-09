using MediatR;
using OpenFindBearings.Application.Behaviors;

namespace OpenFindBearings.Application.Queries.Queries
{
    /// <summary>
    /// 获取角色权限列表查询
    /// </summary>
    public record GetRolePermissionsQuery(Guid RoleId) : IRequest<List<string>>, IQuery;
}
