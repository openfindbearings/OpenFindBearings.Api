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
            // 健康检查
            app.MapHealthChecks("/health");

            // 按模块映射端点
            app.MapPublicEndpoints();      // 公共接口
            app.MapUserEndpoints();        // 用户接口
            app.MapMerchantEndpoints();    // 商家接口
            app.MapAdminEndpoints();       // 管理员接口
            app.MapSyncEndpoints();        // 同步接口
            app.MapMobileEndpoints();      // 移动端接口
        }
    }
}
