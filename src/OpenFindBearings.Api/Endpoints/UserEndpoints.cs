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
            var group = app.MapGroup("/api/me")
                .WithTags("当前用户接口")
                .RequireAuthorization("Authenticated");

            // ============ 1.1 用户基础 ============

            #region 用户基础
            /// <summary>
            /// 获取当前用户资料
            /// </summary>
            group.MapGet("/profile", async (
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
            .WithSummary("获取当前用户资料")
            .WithDescription("获取当前登录用户的昵称、头像、地址、职业、公司、行业等信息");

            /// <summary>
            /// 更新当前用户资料
            /// </summary>
            group.MapPut("/profile", async (
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
            .WithSummary("更新当前用户资料")
            .WithDescription("更新当前登录用户的昵称、头像、地址、职业、公司、行业等信息");

            /// <summary>
            /// 获取当前用户权限列表
            /// </summary>
            group.MapGet("/permissions", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var query = new GetUserPermissionsQuery { UserId = currentUser.UserId.Value };
                var result = await mediator.Send(query);

                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetMyPermissions")
            .WithSummary("获取当前用户权限")
            .WithDescription("获取当前登录用户拥有的所有权限列表");

            /// <summary>
            /// 获取当前用户角色列表
            /// </summary>
            group.MapGet("/roles", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var query = new GetUserRolesQuery { UserId = currentUser.UserId.Value };
                var result = await mediator.Send(query);

                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetMyRoles")
            .WithSummary("获取当前用户角色")
            .WithDescription("获取当前登录用户拥有的所有角色列表");
            #endregion

            // ============ 1.2 轴承收藏 ============

            #region 轴承收藏
            /// <summary>
            /// 获取我的收藏轴承列表
            /// </summary>
            group.MapGet("/favorites/bearings", async (
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
            /// 收藏轴承
            /// </summary>
            group.MapPost("/favorites/bearings/{bearingId:guid}", async (
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
            group.MapDelete("/favorites/bearings/{bearingId:guid}", async (
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
            /// 检查轴承收藏状态
            /// </summary>
            group.MapGet("/favorites/bearings/{bearingId:guid}/check", async (
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
            #endregion

            // ============ 1.3 商家关注 ============

            #region 商家关注
            /// <summary>
            /// 获取我关注的商家列表
            /// </summary>
            group.MapGet("/follows/merchants", async (
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
            /// 关注商家
            /// </summary>
            group.MapPost("/follows/merchants/{merchantId:guid}", async (
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
            group.MapDelete("/follows/merchants/{merchantId:guid}", async (
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
            /// 检查商家关注状态
            /// </summary>
            group.MapGet("/follows/merchants/{merchantId:guid}/check", async (
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
            #endregion

            // ============ 1.4 浏览历史 ============

            #region 浏览历史
            /// <summary>
            /// 获取轴承浏览历史
            /// </summary>
            group.MapGet("/history/bearings", async (
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
            /// 记录轴承浏览（前端自动调用）
            /// </summary>
            group.MapPost("/history/bearings/{bearingId:guid}", async (
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
            /// 获取商家浏览历史
            /// </summary>
            group.MapGet("/history/merchants", async (
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
            /// 记录商家浏览（前端自动调用）
            /// </summary>
            group.MapPost("/history/merchants/{merchantId:guid}", async (
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
            /// 清空浏览历史
            /// </summary>
            group.MapDelete("/history/clear", async (
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
            #endregion

            // ============ 1.5 纠错功能 ============

            #region 纠错功能
            /// <summary>
            /// 获取我的纠错列表
            /// </summary>
            group.MapGet("/corrections", async (
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
            .WithDescription("查看当前用户提交的所有纠错记录（包括审核中、已通过、已拒绝）");

            /// <summary>
            /// 获取我的单条纠错详情
            /// </summary>
            group.MapGet("/corrections/{id:guid}", async (
                Guid id,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var query = new GetMyCorrectionDetailQuery
                {
                    CorrectionId = id,
                    UserId = currentUser.UserId.Value
                };
                var result = await mediator.Send(query);

                return result == null
                    ? ApiResponseHelper.NotFound("纠错不存在", httpContext)
                    : ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetMyCorrectionDetail")
            .WithSummary("获取我的纠错详情")
            .WithDescription("查看当前用户提交的某条纠错详情");

            /// <summary>
            /// 提交轴承纠错
            /// </summary>
            group.MapPost("/bearings/{bearingId:guid}/corrections", async (
                Guid bearingId,
                SubmitBearingCorrectionCommand command,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var submitCommand = command with
                {
                    BearingId = bearingId,
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
            group.MapPost("/merchants/{merchantId:guid}/corrections", async (
                Guid merchantId,
                SubmitMerchantCorrectionCommand command,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var submitCommand = command with
                {
                    MerchantId = merchantId,
                    SubmittedBy = currentUser.UserId.Value
                };

                var result = await mediator.Send(submitCommand);
                return ApiResponseHelper.Ok(new { id = result, message = "纠错已提交" }, httpContext: httpContext);
            })
            .WithName("SubmitMerchantCorrection")
            .WithSummary("提交商家纠错")
            .WithDescription("对商家信息提交纠错请求"); 
            #endregion
        }
    }
}
