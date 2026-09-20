using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Application.Queries.Mobile.CheckVersion;
using OpenFindBearings.Application.Queries.Mobile.GetMobileConfig;

namespace OpenFindBearings.Api.Endpoints
{
    /// <summary>
    /// 移动端专用接口（供 BFF 代理消费）
    /// 改动说明（v1.5.2 僵尸清理）：/bearings/light 与 /home 端点已删除——
    ///   Taro 首页聚合由 BFF /mobile/home 直接调通用端点（bearings/hot、merchants/search、brands、bearing-types），
    ///   不经本组；light 列表全链路零消费。config 与 version/check 由 BFF ConfigEndpoints 代理，保留。
    /// </summary>
    public static class MobileEndpoints
    {
        public static void MapMobileEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/mobile")
                .WithTags("移动端接口")
                .AllowAnonymous();

            /// <summary>
            /// 获取应用配置（站点名/备案/客服/版本/媒体 base，BFF 强类型透传给 Taro）
            /// </summary>
            group.MapGet("/config", async (
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var query = new GetMobileConfigQuery();
                var result = await mediator.Send(query);
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetMobileConfig")
            .WithSummary("获取应用配置")
            .WithDescription("获取移动端应用的版本、更新等配置信息");

            /// <summary>
            /// 检查版本更新
            /// </summary>
            group.MapGet("/version/check", async (
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] string currentVersion,
                [FromQuery] string platform) =>
            {
                var query = new CheckVersionQuery
                {
                    CurrentVersion = currentVersion,
                    Platform = platform
                };
                var result = await mediator.Send(query);
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("CheckVersion")
            .WithSummary("检查版本更新")
            .WithDescription("检查是否有新版本需要更新");
        }
    }
}
