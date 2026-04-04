using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Api.Services;
using OpenFindBearings.Application.Features.Corrections.Commands;
using OpenFindBearings.Application.Features.Corrections.Queries;
using OpenFindBearings.Application.Features.Favorites.Commands;
using OpenFindBearings.Application.Features.Favorites.Queries;
using OpenFindBearings.Application.Features.Follows.Commands;
using OpenFindBearings.Application.Features.Follows.Queries;
using OpenFindBearings.Application.Features.History.Commands;
using OpenFindBearings.Application.Features.History.Queries;
using OpenFindBearings.Application.Features.Users.Commands;
using OpenFindBearings.Application.Features.Users.Queries;

namespace OpenFindBearings.Api.Endpoints
{
    /// <summary>
    /// 用户接口（需登录）
    /// </summary>
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/user")
                .WithTags("用户接口")
                .RequireAuthorization("Authenticated");

            // ============ 2.1 用户基础 ============

            /// <summary>
            /// 获取当前用户信息（包含统计）
            /// </summary>
            group.MapGet("/me", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var query = new GetUserProfileQuery
                {
                    UserId = currentUser.UserId.Value
                };
                var result = await mediator.Send(query);

                return result == null
                    ? ApiResponseHelper.NotFound("用户不存在", httpContext)
                    : ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetCurrentUser")
            .WithSummary("获取当前用户信息")
            .WithDescription("获取当前登录用户的详细信息，包括角色、权限、统计信息等");

            /// <summary>
            /// 获取个人信息
            /// </summary>
            group.MapGet("/me/profile", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var query = new GetUserProfileQuery { UserId = currentUser.UserId.Value };
                var result = await mediator.Send(query);

                return result == null
                    ? ApiResponseHelper.NotFound("用户不存在", httpContext)
                    : ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetUserProfile")
            .WithSummary("获取用户画像")
            .WithDescription("获取用户的职业、行业、公司等信息");

            /// <summary>
            /// 更新个人信息
            /// </summary>
            group.MapPut("/me", async (
                UpdateUserProfileCommand command,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var updateCommand = command with { UserId = currentUser.UserId.Value };
                await mediator.Send(updateCommand);

                return ApiResponseHelper.Ok("更新成功", httpContext);
            })
            .WithName("UpdateUserProfile")
            .WithSummary("更新个人信息")
            .WithDescription("更新用户昵称、头像、电话等信息");

            /// <summary>
            /// 获取用户权限列表
            /// </summary>
            group.MapGet("/me/permissions", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var query = new GetUserPermissionsQuery
                {
                    UserId = currentUser.UserId.Value
                };
                var result = await mediator.Send(query);

                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetMyPermissions") 
            .WithSummary("获取用户权限")
            .WithDescription("获取当前用户拥有的所有权限列表");

            /// <summary>
            /// 获取用户角色
            /// </summary>
            group.MapGet("/me/roles", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var query = new GetUserRolesQuery
                {
                    UserId = currentUser.UserId.Value
                };
                var result = await mediator.Send(query);

                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetMyRoles") 
            .WithSummary("获取用户角色")
            .WithDescription("获取当前用户拥有的所有角色列表");

            // ============ 2.2 轴承收藏 ============

            var favoritesGroup = app.MapGroup("/api/user/favorites")
                .WithTags("收藏功能")
                .RequireAuthorization("Authenticated");

            /// <summary>
            /// 收藏轴承
            /// </summary>
            favoritesGroup.MapPost("/bearings/{bearingId:guid}", async (
                Guid bearingId,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var command = new FavoriteBearingCommand
                {
                    UserId = currentUser.UserId.Value,
                    BearingId = bearingId
                };
                var result = await mediator.Send(command);

                return result
                    ? ApiResponseHelper.Ok("收藏成功", httpContext)
                    : ApiResponseHelper.BadRequest("已收藏过该轴承", httpContext: httpContext);
            })
            .WithName("FavoriteBearing")
            .WithSummary("收藏轴承")
            .WithDescription("收藏指定的轴承");

            /// <summary>
            /// 取消收藏轴承
            /// </summary>
            favoritesGroup.MapDelete("/bearings/{bearingId:guid}", async (
                Guid bearingId,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var command = new UnfavoriteBearingCommand
                {
                    UserId = currentUser.UserId.Value,
                    BearingId = bearingId
                };
                await mediator.Send(command);

                return ApiResponseHelper.Ok("取消收藏成功", httpContext);
            })
            .WithName("UnfavoriteBearing")
            .WithSummary("取消收藏轴承")
            .WithDescription("取消收藏指定的轴承");

            /// <summary>
            /// 获取我的收藏轴承列表
            /// </summary>
            favoritesGroup.MapGet("/bearings", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var query = new GetMyFavoriteBearingsQuery
                {
                    UserId = currentUser.UserId.Value,
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
            .WithName("GetMyFavoriteBearings")
            .WithSummary("获取收藏列表")
            .WithDescription("分页获取当前用户收藏的轴承列表");

            /// <summary>
            /// 检查轴承收藏状态
            /// </summary>
            favoritesGroup.MapGet("/bearings/{bearingId:guid}/check", async (
                Guid bearingId,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var query = new CheckBearingFavoriteQuery
                {
                    UserId = currentUser.UserId.Value,
                    BearingId = bearingId
                };
                var result = await mediator.Send(query);

                return ApiResponseHelper.Ok(new { isFavorited = result }, httpContext: httpContext);
            })
            .WithName("CheckBearingFavorite")
            .WithSummary("检查收藏状态")
            .WithDescription("检查指定轴承是否已被当前用户收藏");

            // ============ 2.3 商家关注 ============

            var followsGroup = app.MapGroup("/api/user/follows")
                .WithTags("关注功能")
                .RequireAuthorization("Authenticated");

            /// <summary>
            /// 关注商家
            /// </summary>
            followsGroup.MapPost("/merchants/{merchantId:guid}", async (
                Guid merchantId,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var command = new FollowMerchantCommand
                {
                    UserId = currentUser.UserId.Value,
                    MerchantId = merchantId
                };
                var result = await mediator.Send(command);

                return result
                    ? ApiResponseHelper.Ok("关注成功", httpContext)
                    : ApiResponseHelper.BadRequest("已关注过该商家", httpContext: httpContext);
            })
            .WithName("FollowMerchant")
            .WithSummary("关注商家")
            .WithDescription("关注指定的商家");

            /// <summary>
            /// 取消关注商家
            /// </summary>
            followsGroup.MapDelete("/merchants/{merchantId:guid}", async (
                Guid merchantId,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var command = new UnfollowMerchantCommand
                {
                    UserId = currentUser.UserId.Value,
                    MerchantId = merchantId
                };
                await mediator.Send(command);

                return ApiResponseHelper.Ok("取消关注成功", httpContext);
            })
            .WithName("UnfollowMerchant")
            .WithSummary("取消关注商家")
            .WithDescription("取消关注指定的商家");

            /// <summary>
            /// 获取我关注的商家列表
            /// </summary>
            followsGroup.MapGet("/merchants", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var query = new GetMyFollowedMerchantsQuery
                {
                    UserId = currentUser.UserId.Value,
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
            .WithName("GetMyFollowedMerchants")
            .WithSummary("获取关注列表")
            .WithDescription("分页获取当前用户关注的商家列表");

            /// <summary>
            /// 检查商家关注状态
            /// </summary>
            followsGroup.MapGet("/merchants/{merchantId:guid}/check", async (
                Guid merchantId,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var query = new CheckMerchantFollowQuery
                {
                    UserId = currentUser.UserId.Value,
                    MerchantId = merchantId
                };
                var result = await mediator.Send(query);

                return ApiResponseHelper.Ok(new { isFollowed = result }, httpContext: httpContext);
            })
            .WithName("CheckMerchantFollow")
            .WithSummary("检查关注状态")
            .WithDescription("检查指定商家是否已被当前用户关注");

            // ============ 2.4 浏览历史 ============

            var historyGroup = app.MapGroup("/api/history")
                .WithTags("浏览历史")
                .RequireAuthorization("Authenticated");

            /// <summary>
            /// 记录轴承浏览
            /// </summary>
            historyGroup.MapPost("/bearings/{bearingId:guid}", async (
                Guid bearingId,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var command = new RecordBearingViewCommand
                {
                    UserId = currentUser.UserId.Value,
                    BearingId = bearingId
                };
                await mediator.Send(command);

                return ApiResponseHelper.Ok(httpContext: httpContext);
            })
            .WithName("RecordBearingView")
            .WithSummary("记录轴承浏览")
            .WithDescription("记录用户查看轴承的历史（前端自动调用）");

            /// <summary>
            /// 获取轴承浏览历史
            /// </summary>
            historyGroup.MapGet("/bearings", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var query = new GetMyBearingHistoryQuery
                {
                    UserId = currentUser.UserId.Value,
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
            .WithName("GetMyBearingHistory")
            .WithSummary("获取轴承浏览历史")
            .WithDescription("分页获取当前用户的轴承浏览历史记录");

            /// <summary>
            /// 记录商家浏览
            /// </summary>
            historyGroup.MapPost("/merchants/{merchantId:guid}", async (
                Guid merchantId,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var command = new RecordMerchantViewCommand
                {
                    UserId = currentUser.UserId.Value,
                    MerchantId = merchantId
                };
                await mediator.Send(command);

                return ApiResponseHelper.Ok(httpContext: httpContext);
            })
            .WithName("RecordMerchantView")
            .WithSummary("记录商家浏览")
            .WithDescription("记录用户查看商家的历史（前端自动调用）");

            /// <summary>
            /// 获取商家浏览历史
            /// </summary>
            historyGroup.MapGet("/merchants", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var query = new GetMyMerchantHistoryQuery
                {
                    UserId = currentUser.UserId.Value,
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
            .WithName("GetMyMerchantHistory")
            .WithSummary("获取商家浏览历史")
            .WithDescription("分页获取当前用户的商家浏览历史记录");

            /// <summary>
            /// 清空浏览历史
            /// </summary>
            historyGroup.MapDelete("/clear", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var command = new ClearHistoryCommand
                {
                    UserId = currentUser.UserId.Value
                };
                await mediator.Send(command);

                return ApiResponseHelper.Ok("历史记录已清空", httpContext);
            })
            .WithName("ClearHistory")
            .WithSummary("清空浏览历史")
            .WithDescription("清空当前用户的所有浏览历史记录");

            // ============ 2.5 纠错功能 ============

            var correctionsGroup = app.MapGroup("/api")
                .WithTags("纠错功能")
                .RequireAuthorization("Authenticated");

            /// <summary>
            /// 提交轴承纠错
            /// </summary>
            correctionsGroup.MapPost("/bearings/{id:guid}/corrections", async (
                Guid id,
                SubmitBearingCorrectionCommand command,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var submitCommand = command with
                {
                    BearingId = id,
                    SubmittedBy = currentUser.UserId.Value
                };

                var result = await mediator.Send(submitCommand);
                return ApiResponseHelper.Ok(new { id = result, message = "纠错已提交" }, httpContext: httpContext);
            })
            .WithName("SubmitBearingCorrection")
            .WithSummary("提交轴承纠错")
            .WithDescription("对轴承信息提交纠错请求");

            /// <summary>
            /// 提交商家纠错
            /// </summary>
            correctionsGroup.MapPost("/merchants/{id:guid}/corrections", async (
                Guid id,
                SubmitMerchantCorrectionCommand command,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var submitCommand = command with
                {
                    MerchantId = id,
                    SubmittedBy = currentUser.UserId.Value
                };

                var result = await mediator.Send(submitCommand);
                return ApiResponseHelper.Ok(new { id = result, message = "纠错已提交" }, httpContext: httpContext);
            })
            .WithName("SubmitMerchantCorrection")
            .WithSummary("提交商家纠错")
            .WithDescription("对商家信息提交纠错请求");

            /// <summary>
            /// 获取轴承纠错历史
            /// </summary>
            correctionsGroup.MapGet("/bearings/{id:guid}/corrections", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20) =>
            {
                var query = new GetBearingCorrectionsQuery
                {
                    BearingId = id,
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
            .WithName("GetBearingCorrections")
            .WithSummary("获取轴承纠错历史")
            .WithDescription("查看指定轴承的所有纠错记录");

            /// <summary>
            /// 获取商家纠错历史
            /// </summary>
            correctionsGroup.MapGet("/merchants/{id:guid}/corrections", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20) =>
            {
                var query = new GetMerchantCorrectionsQuery
                {
                    MerchantId = id,
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
            .WithName("GetMerchantCorrections")
            .WithSummary("获取商家纠错历史")
            .WithDescription("查看指定商家的所有纠错记录");

            /// <summary>
            /// 获取我的纠错列表
            /// </summary>
            correctionsGroup.MapGet("/user/corrections", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var query = new GetMyCorrectionsQuery
                {
                    UserId = currentUser.UserId.Value,
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
            .WithName("GetMyCorrections")
            .WithSummary("获取我的纠错列表")
            .WithDescription("查看当前用户提交的所有纠错记录");
        }
    }
}
