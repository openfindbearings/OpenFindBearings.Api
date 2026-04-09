using MediatR;
using OpenFindBearings.Application.Behaviors;
using OpenFindBearings.Application.DTOs;

namespace OpenFindBearings.Application.Queries.Permissions.GetAllPermissions
{
    /// <summary>
    /// 获取所有权限列表查询
    /// </summary>
    public record GetAllPermissionsQuery : IRequest<List<PermissionDto>>, IQuery;
}
