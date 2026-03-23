using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Api.Services;
using OpenFindBearings.Application.Features.Admin.Commands;
using OpenFindBearings.Application.Features.Admin.Queries;
using OpenFindBearings.Application.Features.Bearings.Commands;
using OpenFindBearings.Application.Features.BearingTypes.Commands;
using OpenFindBearings.Application.Features.Brands.Commands;
using OpenFindBearings.Application.Features.Corrections.Commands;
using OpenFindBearings.Application.Features.Corrections.Queries;
using OpenFindBearings.Application.Features.Merchants.Commands;
using OpenFindBearings.Application.Features.Merchants.Queries;
using OpenFindBearings.Application.Features.SystemConfig.Commands;
using OpenFindBearings.Application.Features.SystemConfig.Queries;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Api.Endpoints
{
    /// <summary>
    /// 管理员接口（需管理员角色）
    /// </summary>
    public static class AdminEndpoints
    {
        public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/admin")
                .WithTags("管理员接口")
                .RequireAuthorization("Admin");

            // ============ 4.1 仪表盘 ============

            /// <summary>
            /// 仪表盘统计
            /// </summary>
            group.MapGet("/dashboard/stats", async (
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var query = new GetDashboardStatsQuery();
                var result = await mediator.Send(query);
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetDashboardStats")
            .WithSummary("获取仪表盘统计")
            .WithDescription("获取首页统计数据");

            /// <summary>
            /// 审计日志
            /// </summary>
            group.MapGet("/audit-logs", async (
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] string? action = null,
                [FromQuery] string? entityType = null,
                [FromQuery] Guid? operatorId = null,
                [FromQuery] DateTime? startDate = null,
                [FromQuery] DateTime? endDate = null,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20) =>
            {
                var query = new GetAuditLogsQuery
                {
                    Action = action,
                    EntityType = entityType,
                    OperatorId = operatorId,
                    StartDate = startDate,
                    EndDate = endDate,
                    Page = page,
                    PageSize = pageSize
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
            .WithName("GetAuditLogs")
            .WithSummary("获取审计日志")
            .WithDescription("查看操作日志");

            // ============ 4.2 轴承管理 ============

            /// <summary>
            /// 创建轴承
            /// </summary>
            group.MapPost("/bearings", async (
                CreateBearingCommand command,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var id = await mediator.Send(command);
                return ApiResponseHelper.Created($"/api/admin/bearings/{id}",
                    new { id },
                    "轴承创建成功",
                    httpContext);
            })
            .WithName("AdminCreateBearing")
            .WithSummary("创建轴承")
            .WithDescription("创建新的轴承型号");

            /// <summary>
            /// 更新轴承
            /// </summary>
            group.MapPut("/bearings/{id:guid}", async (
                Guid id,
                UpdateBearingCommand command,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var updateCommand = command with { Id = id };
                await mediator.Send(updateCommand);
                return ApiResponseHelper.Ok("轴承更新成功", httpContext);
            })
            .WithName("AdminUpdateBearing")
            .WithSummary("更新轴承")
            .WithDescription("更新轴承信息");

            /// <summary>
            /// 删除轴承
            /// </summary>
            group.MapDelete("/bearings/{id:guid}", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var command = new DeleteBearingCommand(id);
                await mediator.Send(command);
                return ApiResponseHelper.Ok("轴承删除成功", httpContext);
            })
            .WithName("AdminDeleteBearing")
            .WithSummary("删除轴承")
            .WithDescription("删除轴承（软删除）");

            // ============ 4.3 商家管理 ============

            /// <summary>
            /// 创建商家
            /// </summary>
            group.MapPost("/merchants", async (
                CreateMerchantCommand command,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var id = await mediator.Send(command);
                return ApiResponseHelper.Created($"/api/admin/merchants/{id}",
                    new { id },
                    "商家创建成功",
                    httpContext);
            })
            .WithName("AdminCreateMerchant")
            .WithSummary("创建商家")
            .WithDescription("创建新商家");

            /// <summary>
            /// 更新商家
            /// </summary>
            group.MapPut("/merchants/{id:guid}", async (
                Guid id,
                UpdateMerchantCommand command,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var updateCommand = command with { Id = id };
                await mediator.Send(updateCommand);
                return ApiResponseHelper.Ok("商家更新成功", httpContext);
            })
            .WithName("AdminUpdateMerchant")
            .WithSummary("更新商家")
            .WithDescription("更新商家信息");

            /// <summary>
            /// 删除商家
            /// </summary>
            group.MapDelete("/merchants/{id:guid}", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var command = new DeleteMerchantCommand(id);
                await mediator.Send(command);
                return ApiResponseHelper.Ok("商家删除成功", httpContext);
            })
            .WithName("AdminDeleteMerchant")
            .WithSummary("删除商家")
            .WithDescription("删除商家");

            /// <summary>
            /// 认证商家
            /// </summary>
            group.MapPost("/merchants/{id:guid}/verify", async (
                Guid id,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var command = new VerifyMerchantCommand(id);
                await mediator.Send(command);
                return ApiResponseHelper.Ok("商家认证成功", httpContext);
            })
            .WithName("VerifyMerchant")
            .WithSummary("认证商家")
            .WithDescription("商家认证审核");

            /// <summary>
            /// 商家列表
            /// </summary>
            group.MapGet("/merchants", async (
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] string? keyword = null,
                [FromQuery] string? city = null,
                [FromQuery] int? type = null,
                [FromQuery] bool? verifiedOnly = null,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20) =>
            {
                var query = new SearchMerchantsQuery
                {
                    Keyword = keyword,
                    City = city,
                    Type = type.HasValue ? (MerchantType?)type : null,
                    VerifiedOnly = verifiedOnly,
                    Page = page,
                    PageSize = pageSize
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
            .WithName("AdminGetMerchants")
            .WithSummary("获取商家列表")
            .WithDescription("管理所有商家");

            // ============ 4.4 纠错审核 ============

            /// <summary>
            /// 待审核纠错
            /// </summary>
            group.MapGet("/corrections/pending", async (
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20) =>
            {
                var query = new GetPendingCorrectionsQuery
                {
                    Page = page,
                    PageSize = pageSize
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
            .WithName("GetPendingCorrections")
            .WithSummary("获取待审核纠错")
            .WithDescription("获取所有待审核纠错");

            /// <summary>
            /// 审核通过纠错
            /// </summary>
            group.MapPost("/corrections/{id:guid}/approve", async (
                Guid id,
                ApproveCorrectionCommand command,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var approveCommand = command with
                {
                    CorrectionId = id,
                    ReviewedBy = currentUser.UserId.Value
                };
                await mediator.Send(approveCommand);
                return ApiResponseHelper.Ok("纠错审核通过", httpContext);
            })
            .WithName("ApproveCorrection")
            .WithSummary("审核通过纠错")
            .WithDescription("通过纠错");

            /// <summary>
            /// 拒绝纠错
            /// </summary>
            group.MapPost("/corrections/{id:guid}/reject", async (
                Guid id,
                RejectCorrectionCommand command,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var rejectCommand = command with
                {
                    CorrectionId = id,
                    ReviewedBy = currentUser.UserId.Value
                };
                await mediator.Send(rejectCommand);
                return ApiResponseHelper.Ok("纠错已拒绝", httpContext);
            })
            .WithName("RejectCorrection")
            .WithSummary("拒绝纠错")
            .WithDescription("拒绝纠错（需填写理由）");

            /// <summary>
            /// 所有纠错记录
            /// </summary>
            group.MapGet("/corrections", async (
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] string? targetType = null,
                [FromQuery] Guid? targetId = null,
                [FromQuery] string? status = null,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20) =>
            {
                var query = new GetCorrectionsQuery  // 使用 GetCorrectionsQuery，不是 GetCorrectionDetailQuery
                {
                    TargetType = targetType,
                    TargetId = targetId,
                    Status = status,
                    Page = page,
                    PageSize = pageSize
                };
                var result = await mediator.Send(query);  // result 是 PagedResult<CorrectionDto>
                return ApiResponseHelper.Paged(
                    result.Items,
                    result.TotalCount,
                    result.Page,
                    result.PageSize,
                    httpContext
                );
            })
            .WithName("GetAllCorrections")
            .WithSummary("获取所有纠错")
            .WithDescription("分页查看所有纠错记录");

            /// <summary>
            /// 获取单个纠错详情
            /// </summary>
            group.MapGet("/corrections/{id:guid}", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var query = new GetCorrectionDetailQuery  // 这个是获取单个详情的
                {
                    Id = id
                };
                var result = await mediator.Send(query);
                return result == null
                    ? ApiResponseHelper.NotFound("纠错不存在", httpContext)
                    : ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetCorrectionDetail")
            .WithSummary("获取纠错详情")
            .WithDescription("获取单个纠错的详细信息");

            // ============ 4.5 品牌/类型管理 ============

            /// <summary>
            /// 创建品牌
            /// </summary>
            group.MapPost("/brands", async (
                CreateBrandCommand command,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var id = await mediator.Send(command);
                return ApiResponseHelper.Created($"/api/admin/brands/{id}",
                    new { id },
                    "品牌创建成功",
                    httpContext);
            })
            .WithName("AdminCreateBrand")
            .WithSummary("创建品牌")
            .WithDescription("添加新品牌");

            /// <summary>
            /// 更新品牌
            /// </summary>
            group.MapPut("/brands/{id:guid}", async (
                Guid id,
                UpdateBrandCommand command,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var updateCommand = command with { Id = id };
                await mediator.Send(updateCommand);
                return ApiResponseHelper.Ok("品牌更新成功", httpContext);
            })
            .WithName("AdminUpdateBrand")
            .WithSummary("更新品牌")
            .WithDescription("更新品牌信息");

            /// <summary>
            /// 创建轴承类型
            /// </summary>
            group.MapPost("/bearing-types", async (
                CreateBearingTypeCommand command,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var id = await mediator.Send(command);
                return ApiResponseHelper.Created($"/api/admin/bearing-types/{id}",
                    new { id },
                    "轴承类型创建成功",
                    httpContext);
            })
            .WithName("AdminCreateBearingType")
            .WithSummary("创建轴承类型")
            .WithDescription("添加新轴承类型");

            /// <summary>
            /// 更新轴承类型
            /// </summary>
            group.MapPut("/bearing-types/{id:guid}", async (
                Guid id,
                UpdateBearingTypeCommand command,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var updateCommand = command with { Id = id };
                await mediator.Send(updateCommand);
                return ApiResponseHelper.Ok("轴承类型更新成功", httpContext);
            })
            .WithName("AdminUpdateBearingType")
            .WithSummary("更新轴承类型")
            .WithDescription("更新轴承类型");

            // ============ 4.6 系统配置 ============

            /// <summary>
            /// 获取系统配置
            /// </summary>
            group.MapGet("/config", async (
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] string? group = null) =>
            {
                var query = new GetSystemConfigsQuery
                {
                    Group = group
                };
                var result = await mediator.Send(query);
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetSystemConfigs")
            .WithSummary("获取系统配置")
            .WithDescription("获取系统配置列表");

            /// <summary>
            /// 更新系统配置
            /// </summary>
            group.MapPut("/config/{key}", async (
                string key,
                UpdateSystemConfigCommand command,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var updateCommand = command with
                {
                    Key = key,
                    UpdatedBy = currentUser.UserId.Value
                };
                await mediator.Send(updateCommand);
                return ApiResponseHelper.Ok("配置更新成功", httpContext);
            })
            .WithName("UpdateSystemConfig")
            .WithSummary("更新系统配置")
            .WithDescription("更新指定配置");

            /// <summary>
            /// 获取价格配置
            /// </summary>
            group.MapGet("/config/price", async (
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var query = new GetPriceConfigQuery();
                var result = await mediator.Send(query);
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetPriceConfig")
            .WithSummary("获取价格配置")
            .WithDescription("获取价格相关配置");

            /// <summary>
            /// 获取待审核的营业执照列表
            /// </summary>
            group.MapGet("/licenses/pending", async (
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20) =>
            {
                var query = new GetPendingLicensesQuery
                {
                    Page = page,
                    PageSize = pageSize
                };
                var result = await mediator.Send(query);
                return ApiResponseHelper.Paged(result.Items, result.TotalCount, result.Page, result.PageSize, httpContext);
            })
            .WithName("GetPendingLicenses")
            .WithSummary("获取待审核营业执照")
            .WithDescription("获取所有待审核的营业执照列表")
            .RequireAuthorization("Admin");

            /// <summary>
            /// 审核通过营业执照
            /// </summary>
            group.MapPost("/licenses/{id:guid}/approve", async (
                Guid id,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var command = new ApproveLicenseCommand
                {
                    VerificationId = id,
                    ReviewedBy = currentUser.UserId.Value
                };
                await mediator.Send(command);
                return ApiResponseHelper.Ok("审核通过，商家已认证", httpContext);
            })
            .WithName("ApproveLicense")
            .WithSummary("审核通过营业执照")
            .WithDescription("通过营业执照审核，商家获得认证")
            .RequireAuthorization("Admin");

            /// <summary>
            /// 审核拒绝营业执照
            /// </summary>
            group.MapPost("/licenses/{id:guid}/reject", async (
                Guid id,
                RejectLicenseRequest request,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var command = new RejectLicenseCommand
                {
                    VerificationId = id,
                    ReviewedBy = currentUser.UserId.Value,
                    Reason = request.Reason
                };
                await mediator.Send(command);
                return ApiResponseHelper.Ok("已拒绝", httpContext);
            })
            .WithName("RejectLicense")
            .WithSummary("审核拒绝营业执照")
            .WithDescription("拒绝营业执照审核，填写拒绝理由")
            .RequireAuthorization("Admin");
        }
    }

    public record RejectLicenseRequest(string Reason);
}
