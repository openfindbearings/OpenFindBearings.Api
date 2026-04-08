using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Api.Services;
using OpenFindBearings.Application.Queries.Mobile.CheckVersion;
using OpenFindBearings.Application.Queries.Mobile.GetMobileBearingLightList;
using OpenFindBearings.Application.Queries.Mobile.GetMobileConfig;
using OpenFindBearings.Application.Queries.Mobile.GetMobileHome;

namespace OpenFindBearings.Api.Endpoints
{
    /// <summary>
    /// 移动端专用接口（轻量级优化）
    /// </summary>
    public static class MobileEndpoints
    {
        public static void MapMobileEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/mobile")
                .WithTags("移动端接口")
                .AllowAnonymous();

            /// <summary>
            /// 轴承轻量列表（移动端专用）
            /// </summary>
            group.MapGet("/bearings/light", async (
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] string? keyword = null,
                [FromQuery] decimal? minInnerDia = null,
                [FromQuery] decimal? maxInnerDia = null,
                [FromQuery] Guid? brandId = null,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 10) =>
            {
                var query = new GetMobileBearingLightListQuery
                {
                    Keyword = keyword,
                    MinInnerDiameter = minInnerDia,
                    MaxInnerDiameter = maxInnerDia,
                    BrandId = brandId,
                    Page = page,
                    PageSize = pageSize
                };

                var result = await mediator.Send(query);

                return ApiResponseHelper.Paged(
                    result.Items.ToList(),
                    result.TotalCount,
                    result.Page,
                    result.PageSize,
                    httpContext
                );
            })
            .WithName("MobileBearingLightList")
            .WithSummary("轴承轻量列表")
            .WithDescription("移动端专用，返回精简的轴承列表数据");

            /// <summary>
            /// 获取应用配置
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

            /// <summary>
            /// 获取首页推荐
            /// </summary>
            group.MapGet("/home", async (
                [FromServices] IMediator mediator,
                [FromServices] ICurrentUserService currentUser,
                HttpContext httpContext) =>
            {
                var isAuthenticated = currentUser.UserId.HasValue;

                var query = new GetMobileHomeQuery
                {
                    IsAuthenticated = isAuthenticated,
                    UserId = currentUser.UserId
                };
                var result = await mediator.Send(query);
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetMobileHome")
            .WithSummary("获取首页推荐")
            .WithDescription("获取移动端首页推荐数据");
        }
    }
}
