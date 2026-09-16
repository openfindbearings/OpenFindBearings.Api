using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Api.Services;
using OpenFindBearings.Application.Commands.Notifications.MarkAllRead;
using OpenFindBearings.Application.Commands.Notifications.MarkNotificationRead;
using OpenFindBearings.Application.Queries.Notifications.GetNotifications;
using OpenFindBearings.Application.Queries.Notifications.GetUnreadCount;

namespace OpenFindBearings.Api.Endpoints
{
    /// <summary>
    /// 站内信端点：消息中心列表、未读数、单条已读、全部已读（均限当前登录用户收件箱）
    /// </summary>
    public static class NotificationEndpoints
    {
        public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/notifications")
                .WithTags("站内信")
                .RequireAuthorization("Authenticated");

            /// <summary>
            /// 收件箱分页列表
            /// </summary>
            group.MapGet("/", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] bool unreadOnly = false,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var query = new GetNotificationsQuery
                {
                    UserId = currentUser.UserId.Value,
                    UnreadOnly = unreadOnly,
                    Page = page,
                    PageSize = pageSize
                };
                var result = await mediator.Send(query);
                return ApiResponseHelper.Paged(result.Items.ToList(), result.TotalCount, result.Page, result.PageSize, httpContext);
            })
            .WithName("GetNotifications")
            .WithSummary("站内信列表")
            .WithDescription("当前用户收件箱分页列表，支持仅未读筛选");

            /// <summary>
            /// 未读数（TabBar 角标轮询）
            /// </summary>
            group.MapGet("/unread-count", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var count = await mediator.Send(new GetUnreadCountQuery { UserId = currentUser.UserId.Value });
                return ApiResponseHelper.Ok(new { count }, httpContext: httpContext);
            })
            .WithName("GetUnreadNotificationCount")
            .WithSummary("站内信未读数")
            .WithDescription("当前用户未读通知条数，供角标展示");

            /// <summary>
            /// 标记单条已读
            /// </summary>
            group.MapPost("/{id:guid}/read", async (
                Guid id,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                await mediator.Send(new MarkNotificationReadCommand { Id = id, UserId = currentUser.UserId.Value });
                return ApiResponseHelper.Ok("已标记已读", httpContext);
            })
            .WithName("MarkNotificationRead")
            .WithSummary("标记单条站内信已读")
            .WithDescription("仅能标记本人收件箱内的通知，否则 404");

            /// <summary>
            /// 全部标记已读
            /// </summary>
            group.MapPost("/read-all", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var affected = await mediator.Send(new MarkAllNotificationsReadCommand { UserId = currentUser.UserId.Value });
                return ApiResponseHelper.Ok(new { affected }, "已全部标记已读", httpContext);
            })
            .WithName("MarkAllNotificationsRead")
            .WithSummary("全部站内信标记已读")
            .WithDescription("批量将当前用户未读通知置为已读，返回影响条数");
        }
    }
}
