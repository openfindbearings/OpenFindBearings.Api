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

    }