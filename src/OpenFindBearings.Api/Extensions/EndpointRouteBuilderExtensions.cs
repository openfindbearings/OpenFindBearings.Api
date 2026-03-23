using OpenFindBearings.Api.Endpoints;

namespace OpenFindBearings.Api.Extensions
{
    /// <summary>
    /// 端点映射扩展方法
    /// </summary>
    public static class EndpointRouteBuilderExtensions
    {
        public static void MapApiEndpoints(this IEndpointRouteBuilder app)
        {
            // ============ 按模块映射端点 ============

            // 公共接口（无需认证）
            app.MapPublicEndpoints();

            // 移动端接口（部分需要认证）
            app.MapMobileEndpoints();

            // 用户接口（需登录）
            app.MapUserEndpoints();

            // 商家管理接口（需商家角色）
            app.MapMerchantEndpoints();

            // 管理员接口（需管理员角色）
            app.MapAdminEndpoints();

            // 同步接口（限 sync_client）
            app.MapSyncEndpoints();
        }
    }
}
