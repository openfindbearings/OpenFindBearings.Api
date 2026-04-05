using OpenFindBearings.Api.Filters;

namespace OpenFindBearings.Api.Extensions
{
    /// <summary>
    /// 端点过滤器扩展方法
    /// </summary>
    public static class EndpointFilterExtensions
    {
        /// <summary>
        /// 需要指定权限
        /// </summary>
        /// <param name="builder">端点构建器</param>
        /// <param name="permissionName">权限名称</param>
        /// <returns>端点构建器</returns>
        public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permissionName)
            where TBuilder : IEndpointConventionBuilder
        {
            return builder.AddEndpointFilter(new PermissionEndpointFilter(permissionName));
        }

        /// <summary>
        /// 需要指定角色
        /// </summary>
        /// <param name="builder">端点构建器</param>
        /// <param name="roleName">角色名称</param>
        /// <returns>端点构建器</returns>
        public static TBuilder RequireRole<TBuilder>(this TBuilder builder, string roleName)
            where TBuilder : IEndpointConventionBuilder
        {
            return builder.AddEndpointFilter(new RoleEndpointFilter(roleName));
        }
    }
}
