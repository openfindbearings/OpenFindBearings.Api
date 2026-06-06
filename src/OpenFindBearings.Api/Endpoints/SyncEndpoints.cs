using MediatR;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Application.Commands.Sync.BatchCreateBearings;
using OpenFindBearings.Application.Commands.Sync.BatchCreateBearingTypes;
using OpenFindBearings.Application.Commands.Sync.BatchCreateInterchanges;
using OpenFindBearings.Application.Commands.Sync.BatchCreateMerchantBearings;
using OpenFindBearings.Application.Commands.Sync.BatchCreateMerchants;
using OpenFindBearings.Application.Commands.Sync.Commands;

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

            group.MapPost("/bearings/batch", async (
                BatchCreateBearingsCommand command,
                IMediator mediator,
                HttpContext httpContext) =>
            {
                var result = await mediator.Send(command);
                return ApiResponseHelper.Ok(result, "轴承批量同步完成", httpContext);
            })
            .WithName("BatchCreateBearings")
            .WithSummary("批量同步轴承");

            group.MapPost("/merchants/batch", async (
                BatchCreateMerchantsCommand command,
                IMediator mediator,
                HttpContext httpContext) =>
            {
                var result = await mediator.Send(command);
                return ApiResponseHelper.Ok(result, "商家批量同步完成", httpContext);
            })
            .WithName("BatchCreateMerchants")
            .WithSummary("批量同步商家");

            group.MapPost("/merchantbearings/batch", async (
                BatchCreateMerchantBearingsCommand command,
                IMediator mediator,
                HttpContext httpContext) =>
            {
                var result = await mediator.Send(command);
                return ApiResponseHelper.Ok(result, "关联批量同步完成", httpContext);
            })
            .WithName("BatchCreateMerchantBearings")
            .WithSummary("批量同步关联");

            group.MapPost("/interchanges/batch", async (
                BatchCreateInterchangesCommand command,
                IMediator mediator,
                HttpContext httpContext) =>
            {
                var result = await mediator.Send(command);
                return ApiResponseHelper.Ok(result, "替代品批量同步完成", httpContext);
            })
            .WithName("BatchCreateInterchanges")
            .WithSummary("批量同步替代品");
        }
    }
}
