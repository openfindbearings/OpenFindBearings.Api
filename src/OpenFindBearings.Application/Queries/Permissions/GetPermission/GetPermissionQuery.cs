using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Permissions.GetPermission
{
    /// <summary>
    /// 获取单个权限查询
    /// </summary>
    public record GetPermissionQuery(Guid Id) : IRequest<PermissionDto?>, IQuery;
}
