using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Application.Queries.Bearings.GetBearingByCode;
using OpenFindBearings.Application.Queries.Bearings.GetBearingInterchanges;
using OpenFindBearings.Application.Queries.Bearings.GetBearingQuery;
using OpenFindBearings.Application.Queries.Bearings.GetHotBearings;
using OpenFindBearings.Application.Queries.Bearings.SearchBearings;
using OpenFindBearings.Application.Queries.BearingTypes.GetAllBearingTypes;
using OpenFindBearings.Application.Queries.Brands.GetAllBrands;
using OpenFindBearings.Application.Queries.Follows.CheckMerchantFollow;
using OpenFindBearings.Application.Queries.MerchantBearings.GetMerchantBearingsByMerchant;
using OpenFindBearings.Application.Queries.Merchants.GetMerchant;
using OpenFindBearings.Application.Queries.Merchants.SearchMerchants;

namespace OpenFindBearings.Api.Endpoints
{
    /// <summary>
    /// 公共接口（无需认证）
    /// 提供轴承搜索、详情、商家信息等公开数据访问
    /// </summary>
    public static class PublicEndpoints
    {
        public static void MapPublicEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api")
                .WithTags("公共接口")
                .AllowAnonymous();

            // ============ 登录方式说明 ============

            /// <summary>
            /// 获取登录方式说明
            /// 返回各种登录方式对应的 Token 端点参数，供前端参考
            /// </summary>
            group.MapGet("/login-methods", async (HttpContext httpContext) =>
            {
                return ApiResponseHelper.Ok(new
                {
                    tokenEndpoint = "/connect/token",
                    logoutEndpoint = "/connect/logout",
                    forgotPasswordEndpoint = "/api/users/forgot-password",
                    resetPasswordEndpoint = "/api/users/reset-password",
                    changePasswordEndpoint = "/api/users/change-password",
                    sendSmsEndpoint = "/api/sms/send",
                    registerEndpoint = "/api/users/register",
                    methods = new[]
                    {
                        new { name = "用户名+密码", grantType = "password", parameters = new[] { "username", "password" } },
                        new { name = "手机号+密码", grantType = "password", parameters = new[] { "username (手机号)", "password" } },
                        new { name = "手机号+验证码", grantType = "phone_code", parameters = new[] { "phone_number", "code" } },
                        new { name = "微信登录", grantType = "wechat", parameters = new[] { "code" } },
                        new { name = "刷新令牌", grantType = "refresh_token", parameters = new[] { "refresh_token" } }
                    }
                }, httpContext: httpContext);
            })
            .WithName("GetLoginMethods")
            .WithSummary("获取登录方式说明")
            .WithDescription("返回各种登录方式对应的Token端点参数");

            // ============ 轴承相关接口 ============

            /// <summary>
            /// 轴承搜索
            /// 支持型号、尺寸范围、品牌等多条件搜索
            /// </summary>
            group.MapGet("/bearings/search", async (
                [AsParameters] SearchBearingsQuery query,
                IMediator mediator,
                HttpContext httpContext) =>
            {
                var result = await mediator.Send(query);
                return ApiResponseHelper.Paged(
                    result.Items.ToList(),
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
            /// 按浏览次数获取热门轴承列表
            /// </summary>
            group.MapGet("/bearings/hot", async (
                IMediator mediator,
                HttpContext httpContext,
                [FromQuery] int count = 10) =>
            {
                var query = new GetHotBearingsQuery
                {
                    Count = count
                };
                var result = await mediator.Send(query);
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetHotBearings")
            .WithSummary("获取热门轴承")
            .WithDescription("按浏览次数获取热门轴承列表");

            /// <summary>
            /// 轴承详情
            /// 获取轴承详细信息，包括技术参数、在售商家等
            /// </summary>
            group.MapGet("/bearings/{id:guid}", async (
                Guid id,
                IMediator mediator,
                HttpContext httpContext) =>
            {
                var userId = httpContext.GetUserId();
                var sessionId = httpContext.GetSessionId();

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
            /// 通过型号查询轴承
            /// 通过轴承现行代号精确查询轴承信息
            /// </summary>
            group.MapGet("/bearings/by-code/{currentCode}", async (
                string currentCode,
                IMediator mediator,
                HttpContext httpContext) =>
            {
                var query = new GetBearingByCodeQuery 
                { 
                    CurrentCode = currentCode 
                };
                var result = await mediator.Send(query);

                return result == null
                    ? ApiResponseHelper.NotFound("轴承型号不存在", httpContext)
                    : ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetBearingByCurrentCode")
            .WithSummary("通过型号查询轴承")
            .WithDescription("通过轴承现行代号精确查询轴承信息");

            /// <summary>
            /// 轴承替代品
            /// 获取当前轴承的可替代型号列表
            /// </summary>
            group.MapGet("/bearings/{id:guid}/interchanges", async (
                Guid id,
                IMediator mediator,
                HttpContext httpContext) =>
            {
                var query = new GetBearingInterchangesQuery
                {
                    BearingId = id
                };
                var result = await mediator.Send(query);
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetBearingInterchanges")
            .WithSummary("获取轴承替代品")
            .WithDescription("获取当前轴承的可替代型号列表");

            // ============ 品牌和类型接口 ============

            /// <summary>
            /// 品牌列表
            /// 获取所有轴承品牌的下拉列表
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
            /// 获取所有轴承类型的下拉列表
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
                    result.Items.ToList(),
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
            /// 获取商家详细信息，包括基本信息、在售产品等
            /// </summary>
            group.MapGet("/merchants/{id:guid}", async (
                Guid id,
                IMediator mediator,
                HttpContext httpContext) =>
            {
                var isAuthenticated = httpContext.GetUserId().HasValue;  // 获取登录状态

                var query = new GetMerchantQuery
                {
                    Id = id,
                    IsAuthenticated = isAuthenticated  // 传递登录状态
                };
                var result = await mediator.Send(query);

                if (result == null)
                    return ApiResponseHelper.NotFound("商家不存在", httpContext);

                var userId = httpContext.GetUserId();
                if (userId.HasValue)
                {
                    var followQuery = new CheckMerchantFollowQuery
                    {
                        UserId = userId.Value,
                        MerchantId = id
                    };
                    var isFollowed = await mediator.Send(followQuery);
                    // result.IsFollowed = isFollowed;
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
                HttpContext httpContext,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20,
                [FromQuery] bool? onlyOnSale = true) =>
            {
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
                    result.Items.ToList(),
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
