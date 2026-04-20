using MediatR;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Application.Commands.Sync.BatchCreateBearings;
using OpenFindBearings.Application.Commands.Sync.BatchCreateBearingTypes;
using OpenFindBearings.Application.Commands.Sync.BatchCreateInterchanges;
using OpenFindBearings.Application.Commands.Sync.BatchCreateMerchantBearings;
using OpenFindBearings.Application.Commands.Sync.BatchCreateMerchants;
using OpenFindBearings.Application.Commands.Sync.Commands;
using OpenFindBearings.Application.Queries.Sync.GetSyncTaskStatus;

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
                .RequireAuthorization("SyncClient");

            // ============ 同步操作（使用 ApiResponseHelper） ============

            group.MapPost("/brands/batch", async (
                BatchCreateBrandsCommand command,
                IMediator mediator,
                HttpContext httpContext) =>
            {
                var result = await mediator.Send(command);
                return ApiResponseHelper.Ok(result, "品牌批量同步完成", httpContext);
            })
            .WithName("SyncBrands")
            .WithSummary("批量同步品牌");

            group.MapPost("/bearingtypes/batch", async (
                BatchCreateBearingTypesCommand command,
                IMediator mediator,
                HttpContext httpContext) =>
            {
                var result = await mediator.Send(command);
                return ApiResponseHelper.Ok(result, "轴承类型批量同步完成", httpContext);
            })
            .WithName("SyncBearingTypes")
            .WithSummary("批量同步轴承类型");

            // ============ 异步操作（不使用 ApiResponseHelper） ============

            group.MapPost("/bearings/batch", async (
                BatchCreateBearingsCommand command,
                IMediator mediator,
                HttpContext httpContext) =>
            {
                var result = await mediator.Send(command);

                var clientId = httpContext.User.FindFirst("client_id")?.Value ?? "unknown";
                httpContext.Items["ClientId"] = clientId;

                var taskId = Guid.NewGuid();
                return ApiResponseHelper.Accepted($"/api/sync/tasks/{taskId}", new { taskId }, httpContext: httpContext);
            })
            .WithName("BatchCreateBearings")
            .WithSummary("批量同步轴承");

            group.MapPost("/merchants/batch", async (
                BatchCreateMerchantsCommand command,
                IMediator mediator,
                HttpContext httpContext) =>
            {
                var result = await mediator.Send(command);
                var taskId = Guid.NewGuid();
                return ApiResponseHelper.Accepted($"/api/sync/tasks/{taskId}", new { taskId }, httpContext: httpContext);
            })
            .WithName("BatchCreateMerchants")
            .WithSummary("批量同步商家");

            group.MapPost("/merchantbearings/batch", async (
                BatchCreateMerchantBearingsCommand command,
                IMediator mediator,
                HttpContext httpContext) =>
            {
                var result = await mediator.Send(command);
                var taskId = Guid.NewGuid();
                return ApiResponseHelper.Accepted($"/api/sync/tasks/{taskId}", new { taskId }, httpContext: httpContext);
            })
            .WithName("BatchCreateMerchantBearings")
            .WithSummary("批量同步关联");

            group.MapPost("/interchanges/batch", async (
                BatchCreateInterchangesCommand command,
                IMediator mediator,
                HttpContext httpContext) =>
            {
                var result = await mediator.Send(command);
                var taskId = Guid.NewGuid();
               return ApiResponseHelper.Accepted($"/api/sync/tasks/{taskId}", new { taskId }, httpContext: httpContext);
            })
            .WithName("BatchCreateInterchanges")
            .WithSummary("批量同步替代品");

            // ============ 任务状态查询（使用 ApiResponseHelper） ============

            group.MapGet("/tasks/{taskId}", async (
                string taskId,
                IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!Guid.TryParse(taskId, out var taskGuid))
                {
                    return ApiResponseHelper.BadRequest("无效的任务ID格式", httpContext: httpContext);
                }

                var query = new GetSyncTaskStatusQuery { TaskId = taskGuid };
                var result = await mediator.Send(query);

                return result == null
                    ? ApiResponseHelper.NotFound($"任务不存在: {taskId}", httpContext)
                    : ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetSyncTaskStatus")
            .WithSummary("获取同步任务状态");
        }
    }
}
