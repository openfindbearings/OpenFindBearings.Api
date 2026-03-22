using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Application.Features.Bearings.Queries;
using OpenFindBearings.Application.Features.BearingTypes.Queries;
using OpenFindBearings.Application.Features.Brands.Queries;
using OpenFindBearings.Application.Features.Follows.Queries;
using OpenFindBearings.Application.Features.History.Commands;
using OpenFindBearings.Application.Features.MerchantBearings.Queries;
using OpenFindBearings.Application.Features.Merchants.Queries;
using OpenFindBearings.Domain.Interfaces;

namespace OpenFindBearings.Api.Endpoints
{
    /// <summary>
    /// 公共接口（无需认证）
    /// </summary>
    public static class PublicEndpoints
    {
        public static void MapPublicEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api")
                .WithTags("公共接口")
                .AllowAnonymous();

            // ============ 轴承相关接口 ============

            /// <summary>
            /// 轴承搜索
            /// </summary>
            group.MapGet("/bearings/search", async (
                [AsParameters] SearchBearingsQuery query,
                IMediator mediator,
                HttpContext httpContext) =>
            {
                var result = await mediator.Send(query);
                return ApiResponseHelper.Paged(
                    result.Items,
                    result.TotalCount,
                    result.Page,
                    result.PageSize,
                    httpContext
                );
            })
            .WithName("SearchBearings")
            .WithSummary("搜索轴承")
            .WithDescription("多条件搜索轴承，支持型号、尺寸范围、品牌等");

            /// <summary>
            /// 热门轴承
            /// </summary>
            group.MapGet("/bearings/hot", async (
                IMediator mediator,
                HttpContext httpContext,
                [FromQuery] int count = 10) =>
            {
                var query = new GetHotBearingsQuery(count);
                var result = await mediator.Send(query);
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetHotBearings")
            .WithSummary("获取热门轴承")
            .WithDescription("按浏览次数获取热门轴承列表");

            /// <summary>
            /// 轴承详情
            /// </summary>
            group.MapGet("/bearings/{id:guid}", async (
                Guid id,
                IMediator mediator,
                [FromServices] ILoggerFactory loggerFactory,
                HttpContext httpContext) =>
            {
                var logger = loggerFactory.CreateLogger("PublicEndpoints");

                // 获取用户信息
                var userId = httpContext.GetUserId();
                var sessionId = httpContext.GetSessionId();

                // 创建查询，传入用户信息用于记录浏览次数
                var query = new GetBearingQuery
                {
                    Id = id,
                    UserId = userId,
                    SessionId = sessionId
                };

                var result = await mediator.Send(query);

                if (result == null)
                    return ApiResponseHelper.NotFound("轴承不存在", httpContext);

                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetBearingById")
            .WithSummary("获取轴承详情")
            .WithDescription("获取轴承详细信息，包括技术参数、在售商家等");

            /// <summary>
            /// 型号查询
            /// </summary>
            group.MapGet("/bearings/by-part/{partNumber}", async (
                string partNumber,
                IMediator mediator,
                HttpContext httpContext) =>
            {
                var query = new GetBearingByPartNumberQuery(partNumber);
                var result = await mediator.Send(query);

                return result == null
                    ? ApiResponseHelper.NotFound("轴承型号不存在", httpContext)
                    : ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetBearingByPartNumber")
            .WithSummary("通过型号查询轴承")
            .WithDescription("通过轴承型号精确查询轴承信息");

            /// <summary>
            /// 轴承替代品
            /// </summary>
            group.MapGet("/bearings/{id:guid}/interchanges", async (
                Guid id,
                IMediator mediator,
                HttpContext httpContext) =>
            {
                var query = new GetBearingInterchangesQuery(id);
                var result = await mediator.Send(query);
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetBearingInterchanges")
            .WithSummary("获取轴承替代品")
            .WithDescription("获取当前轴承的可替代型号列表");

            // ============ 品牌和类型接口 ============

            /// <summary>
            /// 品牌列表
            /// </summary>
            group.MapGet("/brands", async (
                IMediator mediator,
                HttpContext httpContext) =>
            {
                var query = new GetAllBrandsQuery();
                var result = await mediator.Send(query);
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetAllBrands")
            .WithSummary("获取品牌列表")
            .WithDescription("获取所有轴承品牌的下拉列表");

            /// <summary>
            /// 轴承类型列表
            /// </summary>
            group.MapGet("/bearing-types", async (
                IMediator mediator,
                HttpContext httpContext) =>
            {
                var query = new GetAllBearingTypesQuery();
                var result = await mediator.Send(query);
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetAllBearingTypes")
            .WithSummary("获取轴承类型列表")
            .WithDescription("获取所有轴承类型的下拉列表");

            // ============ 商家相关接口 ============

            /// <summary>
            /// 商家搜索
            /// </summary>
            group.MapGet("/merchants/search", async (
                [AsParameters] SearchMerchantsQuery query,
                IMediator mediator,
                HttpContext httpContext) =>
            {
                var result = await mediator.Send(query);
                return ApiResponseHelper.Paged(
                    result.Items,
                    result.TotalCount,
                    result.Page,
                    result.PageSize,
                    httpContext
                );
            })
            .WithName("SearchMerchants")
            .WithSummary("搜索商家")
            .WithDescription("多条件搜索商家，支持名称、城市、类型等");

            /// <summary>
            /// 商家详情
            /// </summary>
            group.MapGet("/merchants/{id:guid}", async (
                Guid id,
                IMediator mediator,
                [FromServices] ILoggerFactory loggerFactory,
                HttpContext httpContext) =>
            {
                var logger = loggerFactory.CreateLogger("PublicEndpoints");
                var query = new GetMerchantQuery(id);
                var result = await mediator.Send(query);

                if (result == null)
                    return ApiResponseHelper.NotFound("商家不存在", httpContext);

                // 如果用户已登录，检查是否已关注该商家
                var userId = httpContext.GetUserId();
                if (userId.HasValue)
                {
                    try
                    {
                        var followQuery = new CheckMerchantFollowQuery
                        {
                            UserId = userId.Value,
                            MerchantId = id
                        };
                        var isFollowed = await mediator.Send(followQuery);

                        // 需要确保 MerchantDetailDto 有 IsFollowed 属性
                        // result.IsFollowed = isFollowed;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "检查商家关注状态失败: MerchantId={MerchantId}, UserId={UserId}", id, userId);
                    }
                }

                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetMerchantById")
            .WithSummary("获取商家详情")
            .WithDescription("获取商家详细信息，包括基本信息、在售产品等");

            /// <summary>
            /// 商家轴承列表
            /// </summary>
            group.MapGet("/merchants/{id:guid}/bearings", async (
                Guid id,
                IMediator mediator,
                [FromServices] ILoggerFactory loggerFactory,
                [FromServices] ISystemConfigRepository configRepository,
                HttpContext httpContext,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20,
                [FromQuery] bool? onlyOnSale = true) =>
            {
                var logger = loggerFactory.CreateLogger("PublicEndpoints");

                // 判断用户是否已登录
                var isAuthenticated = httpContext.GetUserId().HasValue;

                var query = new GetMerchantBearingsByMerchantQuery
                {
                    MerchantId = id,
                    OnlyOnSale = onlyOnSale,
                    Page = page,
                    PageSize = pageSize,
                    IsAuthenticated = isAuthenticated
                };

                var result = await mediator.Send(query);

                return ApiResponseHelper.Paged(
                    result.Items,
                    result.TotalCount,
                    result.Page,
                    result.PageSize,
                    httpContext
                );
            })
            .WithName("GetMerchantBearings")
            .WithSummary("获取商家轴承列表")
            .WithDescription("获取指定商家销售的所有轴承，可按在售状态筛选");
        }
    }
}
