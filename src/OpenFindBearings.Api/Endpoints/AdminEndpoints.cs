using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpenFindBearings.Api.DTOs.Requests;
using OpenFindBearings.Api.Extensions;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Api.Middleware;
using OpenFindBearings.Api.Services;
using OpenFindBearings.Application.Commands.Admin.ApproveLicense;
using OpenFindBearings.Application.Commands.Admin.RejectLicense;
using OpenFindBearings.Application.Commands.Bearings.CreateBearing;
using OpenFindBearings.Application.Commands.Bearings.DeleteBearing;
using OpenFindBearings.Application.Commands.Bearings.UpdateBearing;
using OpenFindBearings.Application.Commands.BearingTypes.CreateBearingType;
using OpenFindBearings.Application.Commands.BearingTypes.UpdateBearingType;
using OpenFindBearings.Application.Commands.Brands.CreateBrand;
using OpenFindBearings.Application.Commands.Brands.UpdateBrand;
using OpenFindBearings.Application.Commands.Corrections.Commands;
using OpenFindBearings.Application.Commands.Merchants.Commands;
using OpenFindBearings.Application.Commands.Merchants.DeleteMerchant;
using OpenFindBearings.Application.Commands.Merchants.VerifyMerchant;
using OpenFindBearings.Application.Commands.Permissions.CreatePermission;
using OpenFindBearings.Application.Commands.Permissions.DeletePermission;
using OpenFindBearings.Application.Commands.Permissions.UpdatePermission;
using OpenFindBearings.Application.Commands.Roles.AssignPermissionsToRole;
using OpenFindBearings.Application.Commands.Roles.AssignRoleToUser;
using OpenFindBearings.Application.Commands.Roles.Commands;
using OpenFindBearings.Application.Commands.Roles.DeleteRole;
using OpenFindBearings.Application.Commands.Roles.RemoveRoleFromUser;
using OpenFindBearings.Application.Commands.Roles.UpdateRole;
using OpenFindBearings.Application.Commands.SystemConfig.UpdateSystemConfig;
using OpenFindBearings.Application.Queries.Admin.GetAuditLogs;
using OpenFindBearings.Application.Queries.Admin.GetDashboardStats;
using OpenFindBearings.Application.Queries.Admin.GetPendingLicenses;
using OpenFindBearings.Application.Queries.Corrections.GetCorrectionDetail;
using OpenFindBearings.Application.Queries.Corrections.GetCorrections;
using OpenFindBearings.Application.Queries.Corrections.GetPendingCorrections;
using OpenFindBearings.Application.Queries.Merchants.SearchMerchants;
using OpenFindBearings.Application.Queries.Permissions.GetPermission;
using OpenFindBearings.Application.Queries.Permissions.GetPermissions;
using OpenFindBearings.Application.Queries.Queries;
using OpenFindBearings.Application.Queries.Roles.GetAllRoles;
using OpenFindBearings.Application.Queries.Roles.GetRoleDetail;
using OpenFindBearings.Application.Queries.Roles.GetRoles;
using OpenFindBearings.Application.Queries.SystemConfig.GetPriceConfig;
using OpenFindBearings.Application.Queries.SystemConfig.GetSystemConfigs;
using OpenFindBearings.Application.Queries.Users.GetUserPermissions;
using OpenFindBearings.Application.Queries.Users.GetUserRoles;
using OpenFindBearings.Domain.Enums;
using OpenFindBearings.Domain.Repositories;

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
                    result.Items.ToList(),
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
            .WithDescription("创建新的轴承型号")
            .RequirePermission("product.create");

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
            .WithDescription("更新轴承信息")
            .RequirePermission("product.edit");

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
            .WithDescription("删除轴承（软删除）")
            .RequirePermission("product.delete");

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
            .WithDescription("创建新商家")
            .RequirePermission("merchant.manage");

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
            .WithDescription("更新商家信息")
            .RequirePermission("merchant.manage");

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
            .WithDescription("删除商家")
            .RequirePermission("merchant.manage");

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
            .WithDescription("商家认证审核")
            .RequirePermission("merchant.verify");

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
                    result.Items.ToList(),
                    result.TotalCount,
                    result.Page,
                    result.PageSize,
                    httpContext
                );
            })
            .WithName("AdminGetMerchants")
            .WithSummary("获取商家列表")
            .WithDescription("管理所有商家")
            .RequirePermission("merchant.view");

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
                    result.Items.ToList(),
                    result.TotalCount,
                    result.Page,
                    result.PageSize,
                    httpContext
                );
            })
            .WithName("GetPendingCorrections")
            .WithSummary("获取待审核纠错")
            .WithDescription("获取所有待审核纠错")
            .RequirePermission("correction.review");

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
            .WithDescription("通过纠错")
            .RequirePermission("correction.review");

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
            .WithDescription("拒绝纠错（需填写理由）")
            .RequirePermission("correction.review");

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
                var query = new GetCorrectionsQuery
                {
                    TargetType = targetType,
                    TargetId = targetId,
                    Status = status,
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
            .WithName("GetAllCorrections")
            .WithSummary("获取所有纠错")
            .WithDescription("分页查看所有纠错记录")
            .RequirePermission("correction.review");

            /// <summary>
            /// 获取单个纠错详情
            /// </summary>
            group.MapGet("/corrections/{id:guid}", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var query = new GetCorrectionDetailQuery
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
            .WithDescription("获取单个纠错的详细信息")
            .RequirePermission("correction.review");

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
            .WithDescription("添加新品牌")
            .RequirePermission("product.create");

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
            .WithDescription("更新品牌信息")
            .RequirePermission("product.edit");

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
            .WithDescription("添加新轴承类型")
            .RequirePermission("product.create");

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
            .WithDescription("更新轴承类型")
            .RequirePermission("product.edit");

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
            .WithDescription("更新指定配置")
            .RequirePermission("system.manage");

            /// <summary>
            /// 刷新限流配置缓存
            /// </summary>
            group.MapPost("/admin/cache/refresh-rate-limit", async (
                [FromServices] ISystemConfigRepository configRepo,
                HttpContext httpContext) =>
            {
                // 刷新中间件缓存
                RateLimitingMiddleware.RefreshCache();

                // 可选：同时从数据库读取最新配置验证
                var guestLimit = await configRepo.GetValueAsync("RateLimit.Guest.RequestsPerMinute", 30);
                var userLimit = await configRepo.GetValueAsync("RateLimit.User.RequestsPerMinute", 60);

                return ApiResponseHelper.Ok(new
                {
                    message = "限流配置缓存已刷新",
                    newConfig = new { guestLimit, userLimit }
                }, httpContext: httpContext);
            })
            .WithName("RefreshRateLimitCache")
            .WithSummary("刷新限流配置缓存")
            .WithDescription("修改限流配置后调用此接口，使配置立即生效")
            .RequireAuthorization("Admin");

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

                return ApiResponseHelper.Paged(
                    result.Items.ToList(),
                    result.TotalCount,
                    result.Page,
                    result.PageSize,
                    httpContext);
            })
            .WithName("GetPendingLicenses")
            .WithSummary("获取待审核营业执照")
            .WithDescription("获取所有待审核的营业执照列表")
            .RequireAuthorization("Admin")
            .RequirePermission("merchant.verify");

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
            .RequireAuthorization("Admin")
            .RequirePermission("merchant.verify");

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
            .RequireAuthorization("Admin")
            .RequirePermission("merchant.verify");

            // ============ 角色管理 ============

            /// <summary>
            /// 获取角色列表（分页）
            /// </summary>
            group.MapGet("/roles", async (
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20,
                [FromQuery] string? keyword = null) =>
            {
                var query = new GetRolesQuery
                {
                    Page = page,
                    PageSize = pageSize,
                    Keyword = keyword
                };
                var result = await mediator.Send(query);
                return ApiResponseHelper.Paged(result.Items.ToList(), result.TotalCount, result.Page, result.PageSize, httpContext);
            })
            .WithName("GetRoles")
            .WithSummary("获取角色列表")
            .WithDescription("分页获取所有角色列表")
            .RequireAuthorization("Admin")
            .RequirePermission("role.manage");

            /// <summary>
            /// 获取角色详情
            /// </summary>
            group.MapGet("/roles/{id:guid}", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var query = new GetRoleDetailQuery(id);
                var result = await mediator.Send(query);
                return result == null
                    ? ApiResponseHelper.NotFound("角色不存在", httpContext)
                    : ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetRoleDetail")
            .WithSummary("获取角色详情")
            .WithDescription("获取角色详细信息，包括拥有的权限")
            .RequireAuthorization("Admin")
            .RequirePermission("role.manage");

            /// <summary>
            /// 获取角色列表（全部，用于下拉框）
            /// </summary>
            group.MapGet("/roles/all", async (
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var query = new GetAllRolesQuery();
                var result = await mediator.Send(query);
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetAllRoles")
            .WithSummary("获取所有角色")
            .WithDescription("获取所有角色列表，用于下拉框")
            .RequireAuthorization("Admin")
            .RequirePermission("role.manage");

            /// <summary>
            /// 创建角色
            /// </summary>
            group.MapPost("/roles", async (
                CreateRoleRequest request,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var command = new CreateRoleCommand
                {
                    Name = request.Name,
                    Description = request.Description
                };
                var id = await mediator.Send(command);
                return ApiResponseHelper.Ok(new { id }, "角色创建成功", httpContext);
            })
            .WithName("CreateRole")
            .WithSummary("创建角色")
            .WithDescription("创建新角色")
            .RequireAuthorization("Admin")
            .RequirePermission("role.manage");

            /// <summary>
            /// 更新角色
            /// </summary>
            group.MapPut("/roles/{id:guid}", async (
                Guid id,
                UpdateRoleRequest request,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var command = new UpdateRoleCommand
                {
                    Id = id,
                    Name = request.Name,
                    Description = request.Description
                };
                await mediator.Send(command);
                return ApiResponseHelper.Ok("角色更新成功", httpContext);
            })
            .WithName("UpdateRole")
            .WithSummary("更新角色")
            .WithDescription("更新角色信息")
            .RequireAuthorization("Admin")
            .RequirePermission("role.manage");

            /// <summary>
            /// 删除角色
            /// </summary>
            group.MapDelete("/roles/{id:guid}", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var command = new DeleteRoleCommand(id);
                await mediator.Send(command);
                return ApiResponseHelper.Ok("角色删除成功", httpContext);
            })
            .WithName("DeleteRole")
            .WithSummary("删除角色")
            .WithDescription("删除角色（不能删除系统角色）")
            .RequireAuthorization("Admin")
            .RequirePermission("role.manage");

            /// <summary>
            /// 为角色分配权限
            /// </summary>
            group.MapPost("/roles/{id:guid}/permissions", async (
                Guid id,
                AssignPermissionsRequest request,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var command = new AssignPermissionsToRoleCommand
                {
                    RoleId = id,
                    PermissionNames = request.PermissionNames
                };
                await mediator.Send(command);
                return ApiResponseHelper.Ok("权限分配成功", httpContext);
            })
            .WithName("AssignPermissionsToRole")
            .WithSummary("分配权限")
            .WithDescription("为角色分配权限")
            .RequireAuthorization("Admin")
            .RequirePermission("role.manage");

            /// <summary>
            /// 获取角色的权限列表
            /// </summary>
            group.MapGet("/roles/{id:guid}/permissions", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var query = new GetRolePermissionsQuery(id);
                var result = await mediator.Send(query);
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetRolePermissions")
            .WithSummary("获取角色权限")
            .WithDescription("获取角色拥有的权限名称列表")
            .RequireAuthorization("Admin")
            .RequirePermission("role.manage");

            // ============ 权限管理 ============

            /// <summary>
            /// 获取权限列表
            /// </summary>
            group.MapGet("/permissions", async (
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20,
                [FromQuery] string? group = null) =>
            {
                var query = new GetPermissionsQuery
                {
                    Page = page,
                    PageSize = pageSize,
                    Group = group
                };
                var result = await mediator.Send(query);
                return ApiResponseHelper.Paged(result.Items.ToList(), result.TotalCount, result.Page, result.PageSize, httpContext);
            })
            .WithName("GetPermissions")
            .WithSummary("获取权限列表")
            .WithDescription("分页获取所有权限列表")
            .RequireAuthorization("Admin")
            .RequirePermission("role.manage");

            /// <summary>
            /// 获取权限详情
            /// </summary>
            group.MapGet("/permissions/{id:guid}", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var query = new GetPermissionQuery(id);
                var result = await mediator.Send(query);
                return result == null
                    ? ApiResponseHelper.NotFound("权限不存在", httpContext)
                    : ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetPermissionDetail")
            .WithSummary("获取权限详情")
            .WithDescription("获取权限详细信息")
            .RequireAuthorization("Admin")
            .RequirePermission("role.manage");

            /// <summary>
            /// 创建权限
            /// </summary>
            group.MapPost("/permissions", async (
                CreatePermissionRequest request,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var command = new CreatePermissionCommand
                {
                    Name = request.Name,
                    Description = request.Description
                };
                var id = await mediator.Send(command);
                return ApiResponseHelper.Ok(new { id }, "权限创建成功", httpContext);
            })
            .WithName("CreatePermission")
            .WithSummary("创建权限")
            .WithDescription("创建新权限")
            .RequireAuthorization("Admin")
            .RequirePermission("role.manage");

            /// <summary>
            /// 更新权限
            /// </summary>
            group.MapPut("/permissions/{id:guid}", async (
                Guid id,
                UpdatePermissionRequest request,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var command = new UpdatePermissionCommand
                {
                    Id = id,
                    Name = request.Name,
                    Description = request.Description
                };
                await mediator.Send(command);
                return ApiResponseHelper.Ok("权限更新成功", httpContext);
            })
            .WithName("UpdatePermission")
            .WithSummary("更新权限")
            .WithDescription("更新权限信息")
            .RequireAuthorization("Admin")
            .RequirePermission("role.manage");

            /// <summary>
            /// 删除权限
            /// </summary>
            group.MapDelete("/permissions/{id:guid}", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var command = new DeletePermissionCommand(id);
                await mediator.Send(command);
                return ApiResponseHelper.Ok("权限删除成功", httpContext);
            })
            .WithName("DeletePermission")
            .WithSummary("删除权限")
            .WithDescription("删除权限（不能删除已被使用的权限）")
            .RequireAuthorization("Admin")
            .RequirePermission("role.manage");

            // ============ 用户角色管理 ============

            /// <summary>
            /// 分配角色给用户
            /// </summary>
            group.MapPost("/users/{userId:guid}/roles", async (
                Guid userId,
                AssignUserRoleRequest request,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var command = new AssignRoleToUserCommand
                {
                    UserId = userId,
                    RoleName = request.RoleName
                };
                await mediator.Send(command);
                return ApiResponseHelper.Ok("角色分配成功", httpContext);
            })
            .WithName("AdminAssignRoleToUser")
            .WithSummary("分配角色")
            .WithDescription("分配角色给用户")
            .RequireAuthorization("Admin")
            .RequirePermission("user.manage");

            /// <summary>
            /// 移除用户角色
            /// </summary>
            group.MapDelete("/users/{userId:guid}/roles/{roleName}", async (
                Guid userId,
                string roleName,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var command = new RemoveRoleFromUserCommand
                {
                    UserId = userId,
                    RoleName = roleName
                };
                await mediator.Send(command);
                return ApiResponseHelper.Ok("角色移除成功", httpContext);
            })
            .WithName("AdminRemoveRoleFromUser")
            .WithSummary("移除角色")
            .WithDescription("从用户移除角色")
            .RequireAuthorization("Admin")
            .RequirePermission("user.manage");

            /// <summary>
            /// 获取用户的角色列表
            /// </summary>
            group.MapGet("/users/{userId:guid}/roles", async (
                Guid userId,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var query = new GetUserRolesQuery { UserId = userId };
                var result = await mediator.Send(query);
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("AdminGetUserRoles")
            .WithSummary("获取用户角色")
            .WithDescription("获取用户的角色列表")
            .RequireAuthorization("Admin")
            .RequirePermission("user.manage");

            /// <summary>
            /// 获取用户的权限列表
            /// </summary>
            group.MapGet("/users/{userId:guid}/permissions", async (
                Guid userId,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var query = new GetUserPermissionsQuery { UserId = userId };
                var result = await mediator.Send(query);
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("AdminGetUserPermissions")
            .WithSummary("获取用户权限")
            .WithDescription("获取用户的权限列表")
            .RequireAuthorization("Admin")
            .RequirePermission("user.manage");
        }
    }
}
