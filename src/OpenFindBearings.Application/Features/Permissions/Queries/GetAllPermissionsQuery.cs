using MediatR;
using OpenFindBearings.Application.Features.Permissions.DTOs;

namespace OpenFindBearings.Application.Features.Permissions.Queries
{
    /// <summary>
    /// 获取所有权限列表查询
    /// </summary>
    public record GetAllPermissionsQuery : IRequest<List<PermissionDto>>;
}
