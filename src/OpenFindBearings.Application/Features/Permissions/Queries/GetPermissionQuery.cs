using MediatR;
using OpenFindBearings.Application.Features.Permissions.DTOs;

namespace OpenFindBearings.Application.Features.Permissions.Queries
{
    /// <summary>
    /// 获取单个权限查询
    /// </summary>
    public record GetPermissionQuery(Guid Id) : IRequest<PermissionDto?>;
}
