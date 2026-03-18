using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFindBearings.Application.Features.Permissions.Queries
{
    /// <summary>
    /// 检查用户是否有指定权限
    /// </summary>
    public record CheckUserPermissionQuery(
        string AuthUserId,
        string PermissionName
    ) : IRequest<bool>;
}
