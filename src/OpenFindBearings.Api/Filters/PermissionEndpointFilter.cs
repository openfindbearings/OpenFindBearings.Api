using OpenFindBearings.Api.Services;

namespace OpenFindBearings.Api.Filters
{
    /// <summary>
    /// 权限端点过滤器
    /// 要求用户已认证（RequireAuthorization 门禁在前）+ 校验具体权限
    /// </summary>
    public class PermissionEndpointFilter : IEndpointFilter
    {
        private readonly string _permissionName;

        public PermissionEndpointFilter(string permissionName)
        {
            _permissionName = permissionName;
        }

        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var httpContext = context.HttpContext;
            var currentUser = httpContext.RequestServices.GetRequiredService<ICurrentUserService>();

            if (!currentUser.IsAuthenticated)
            {
                return Results.Unauthorized();
            }

            var permissionService = httpContext.RequestServices.GetRequiredService<IPermissionService>();
            var hasPermission = await permissionService.HasPermissionAsync(_permissionName);

            if (!hasPermission)
            {
                return Results.Forbid();
            }

            return await next(context);
        }
    }

    /// <summary>
    /// 角色端点过滤器（API 层）
    /// 要求用户已认证 + 校验角色
    /// </summary>
    public class RoleEndpointFilter : IEndpointFilter
    {
        private readonly string _roleName;

        public RoleEndpointFilter(string roleName)
        {
            _roleName = roleName;
        }

        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var httpContext = context.HttpContext;
            var currentUser = httpContext.RequestServices.GetRequiredService<ICurrentUserService>();

            if (!currentUser.IsAuthenticated)
            {
                return Results.Unauthorized();
            }

            var permissionService = httpContext.RequestServices.GetRequiredService<IPermissionService>();
            var hasRole = permissionService.HasRole(_roleName);

            if (!hasRole)
            {
                return Results.Forbid();
            }

            return await next(context);
        }
    }
}
