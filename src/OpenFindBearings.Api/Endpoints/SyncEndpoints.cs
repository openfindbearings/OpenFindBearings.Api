using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpenFindBearings.Api.DTOs;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Application.Features.Sync.Commands;
using OpenFindBearings.Application.Features.Sync.DTOs;
using OpenFindBearings.Application.Features.Sync.Queries;

namespace OpenFindBearings.Api.Endpoints
{
    /// <summary>
    /// 数据同步接口（使用 OpenIddict 客户端认证）
    /// </summary>
    public static class SyncEndpoints
    {
        public static void MapSyncEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/sync")
                .WithTags("数据同步接口")
                .RequireAuthorization("SyncClient"); // 使用策略认证，不需要自定义 Filter

            /// <summary>
            /// 批量同步轴承
            /// </summary>
            group.MapPost("/bearings/batch", async (
                BatchCreateBearingsCommand command,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var result = await mediator.Send(command);

                // 记录客户端信息到日志
                var clientId = httpContext.User.FindFirst("client_id")?.Value ?? "unknown";
                httpContext.Items["ClientId"] = clientId;

                return Results.Accepted(
                    $"/api/sync/tasks/{Guid.NewGuid()}",
                    ApiResponseHelper.Ok(result, "批量同步任务已接受", httpContext)
                );
            })
            .WithName("BatchCreateBearings")
            .WithSummary("批量同步轴承")
            .WithDescription("批量创建/更新轴承数据（需客户端认证）")
            .Produces<ApiResponse<BatchResult>>(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

            /// <summary>
            /// 批量同步商家
            /// </summary>
            group.MapPost("/merchants/batch", async (
                BatchCreateMerchantsCommand command,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var result = await mediator.Send(command);

                return Results.Accepted(
                    $"/api/sync/tasks/{Guid.NewGuid()}",
                    ApiResponseHelper.Ok(result, "批量同步任务已接受", httpContext)
                );
            })
            .WithName("BatchCreateMerchants")
            .WithSummary("批量同步商家")
            .WithDescription("批量创建/更新商家数据（需客户端认证）")
            .Produces<ApiResponse<BatchResult>>(StatusCodes.Status202Accepted);

            /// <summary>
            /// 批量同步商家-轴承关联
            /// </summary>
            group.MapPost("/merchant-bearings/batch", async (
                BatchCreateMerchantBearingsCommand command,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var result = await mediator.Send(command);

                return Results.Accepted(
                    $"/api/sync/tasks/{Guid.NewGuid()}",
                    ApiResponseHelper.Ok(result, "批量同步任务已接受", httpContext)
                );
            })
            .WithName("BatchCreateMerchantBearings")
            .WithSummary("批量同步关联")
            .WithDescription("批量创建/更新商家-轴承关联数据（需客户端认证）")
            .Produces<ApiResponse<BatchResult>>(StatusCodes.Status202Accepted);

            /// <summary>
            /// 批量同步替代品关系
            /// </summary>
            group.MapPost("/interchanges/batch", async (
                BatchCreateInterchangesCommand command,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var result = await mediator.Send(command);

                return Results.Accepted(
                    $"/api/sync/tasks/{Guid.NewGuid()}",
                    ApiResponseHelper.Ok(result, "批量同步任务已接受", httpContext)
                );
            })
            .WithName("BatchCreateInterchanges")
            .WithSummary("批量同步替代品")
            .WithDescription("批量创建/更新轴承替代品关系（需客户端认证）")
            .Produces<ApiResponse<BatchResult>>(StatusCodes.Status202Accepted);

            /// <summary>
            /// 获取同步任务状态
            /// </summary>
            group.MapGet("/tasks/{taskId}", async (
                string taskId,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                // 将 string 类型的 taskId 转换为 Guid
                if (!Guid.TryParse(taskId, out var taskGuid))
                {
                    return ApiResponseHelper.BadRequest("无效的任务ID格式", httpContext: httpContext);
                }

                var query = new GetSyncTaskStatusQuery { TaskId = taskGuid }; // 使用 Guid 构造函数
                var result = await mediator.Send(query);

                return result == null
                    ? ApiResponseHelper.NotFound($"任务不存在: {taskId}", httpContext)
                    : ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetSyncTaskStatus")
            .WithSummary("获取同步任务状态")
            .WithDescription("获取批量任务的处理状态")
            .Produces<ApiResponse<SyncResultDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
        }
    }
}
