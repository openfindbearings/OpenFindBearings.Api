using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using OpenFindBearings.Api.Extensions;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Api.Middleware;
using OpenFindBearings.Api.Services;
using OpenFindBearings.Application.Commands.Admin.ApproveDocument;
using OpenFindBearings.Application.Commands.Admin.RejectDocument;
using OpenFindBearings.Application.Commands.Merchants.AssignMerchantMember;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Application.Commands.Bearings.CreateBearing;
using OpenFindBearings.Application.Commands.Bearings.DeleteBearing;
using OpenFindBearings.Application.Commands.Bearings.HardDeleteBearing;
using OpenFindBearings.Application.Commands.Bearings.RestoreBearing;
using OpenFindBearings.Application.Commands.Bearings.UpdateBearing;
using OpenFindBearings.Application.Commands.BearingTypes.CreateBearingType;
using OpenFindBearings.Application.Commands.BearingTypes.DeleteBearingType;
using OpenFindBearings.Application.Commands.BearingTypes.HardDeleteBearingType;
using OpenFindBearings.Application.Commands.BearingTypes.RestoreBearingType;
using OpenFindBearings.Application.Commands.BearingTypes.UpdateBearingType;
using OpenFindBearings.Application.Commands.Brands.CreateBrand;
using OpenFindBearings.Application.Commands.Brands.DeleteBrand;
using OpenFindBearings.Application.Commands.Brands.HardDeleteBrand;
using OpenFindBearings.Application.Commands.Brands.RestoreBrand;
using OpenFindBearings.Application.Commands.Brands.UpdateBrand;
using OpenFindBearings.Application.Commands.Corrections.Commands;
using OpenFindBearings.Application.Commands.Merchants.Commands;
using OpenFindBearings.Application.Commands.Merchants.ApproveMerchant;
using OpenFindBearings.Application.Commands.Merchants.DeleteMerchant;
using OpenFindBearings.Application.Commands.Merchants.HardDeleteMerchant;
using OpenFindBearings.Application.Commands.Merchants.DetachMerchant;
using OpenFindBearings.Application.Commands.Merchants.RejectMerchant;
using OpenFindBearings.Application.Commands.Merchants.VerifyMerchant;
using OpenFindBearings.Application.Commands.Admin.ApproveMerchantBearing;
using OpenFindBearings.Application.Commands.Admin.RejectMerchantBearing;
using OpenFindBearings.Application.Queries.Admin.GetPendingMerchantBearings;
using OpenFindBearings.Application.Commands.Interchanges.CreateInterchange;
using OpenFindBearings.Application.Commands.Interchanges.UpdateInterchange;
using OpenFindBearings.Application.Commands.Interchanges.DeleteInterchange;
using OpenFindBearings.Application.Queries.Admin.GetInterchanges;
using OpenFindBearings.Application.Commands.Merchants.RestoreMerchant;
using OpenFindBearings.Application.Commands.Roles.AssignPermissionsToRole;
using OpenFindBearings.Application.Commands.Roles.AssignRoleToUser;
using OpenFindBearings.Application.Commands.Roles.Commands;
using OpenFindBearings.Application.Commands.Roles.DeleteRole;
using OpenFindBearings.Application.Commands.Roles.RemoveRoleFromUser;
using OpenFindBearings.Application.Commands.Roles.UpdateRole;
using OpenFindBearings.Application.Commands.SystemConfig.UpdateSystemConfig;
using OpenFindBearings.Application.Queries.Admin.GetAuditLogs;
using OpenFindBearings.Application.Queries.Admin.GetDashboardStats;
using OpenFindBearings.Application.Queries.Admin.GetPendingDocuments;
using OpenFindBearings.Application.Queries.Admin.GetMerchantDocuments;
using OpenFindBearings.Application.Queries.BearingTypes.GetAllBearingTypes;
using OpenFindBearings.Application.Queries.Bearings.SearchBearings;
using OpenFindBearings.Application.Queries.Brands.GetAllBrands;
using OpenFindBearings.Application.Queries.Corrections.GetCorrectionDetail;
using OpenFindBearings.Application.Queries.Corrections.GetCorrections;
using OpenFindBearings.Application.Queries.Corrections.GetPendingCorrections;
using OpenFindBearings.Application.Queries.Merchants.GetMerchant;
using OpenFindBearings.Application.Queries.Merchants.SearchMerchants;
using OpenFindBearings.Application.Queries.Permissions.GetPermission;
using OpenFindBearings.Application.Queries.Permissions.GetPermissions;
using OpenFindBearings.Application.Queries.Queries;
using OpenFindBearings.Application.Queries.Roles.GetAllRoles;
using OpenFindBearings.Application.Queries.Roles.GetRoleDetail;
using OpenFindBearings.Application.Queries.Roles.GetRoles;
using OpenFindBearings.Application.Queries.SystemConfig.GetSystemConfigs;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Application.Commands.Users.ProvisionUser;
using OpenFindBearings.Application.Queries.Users.GetAllPlatformRoles;
using OpenFindBearings.Application.Queries.Users.GetUserByAuthId;
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
                // 双重防线：RequireAuthorization("Admin") 要求已认证 + RequirePermission 校验具体权限

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
            .WithDescription("获取首页统计数据")
            .RequirePermission("dashboard.view");

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
            .WithDescription("查看操作日志")
            .RequirePermission("audit.view");

            // ============ 4.1b 商品审核（v2.15.0：商家上架商品的人工审核出口） ============

            /// <summary>
            /// 待审核商家商品列表
            /// </summary>
            group.MapGet("/merchant-bearings/pending", async (
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20) =>
            {
                var query = new GetPendingMerchantBearingsQuery { Page = page, PageSize = pageSize };
                var result = await mediator.Send(query);
                return ApiResponseHelper.Paged(
                    result.Items.ToList(),
                    result.TotalCount,
                    result.Page,
                    result.PageSize,
                    httpContext
                );
            })
            .WithName("GetPendingMerchantBearings")
            .WithSummary("待审核商家商品")
            .WithDescription("获取待平台审核的商家上架商品")
            .RequirePermission("merchant.verify");

            /// <summary>
            /// 审核通过商家商品（通过即在售，公域可见）
            /// </summary>
            group.MapPost("/merchant-bearings/{id:guid}/approve", async (
                Guid id,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                await mediator.Send(new ApproveMerchantBearingCommand
                {
                    MerchantBearingId = id,
                    ReviewedBy = currentUser.UserId ?? Guid.Empty
                });
                return ApiResponseHelper.Ok("审核通过", httpContext);
            })
            .WithName("ApproveMerchantBearing")
            .WithSummary("审核通过商家商品")
            .WithDescription("通过商家商品审核（通过即转在售并对公域可见）")
            .RequirePermission("merchant.verify");

            /// <summary>
            /// 驳回商家商品（附理由，商家可见并可修改重提）
            /// </summary>
            group.MapPost("/merchant-bearings/{id:guid}/reject", async (
                Guid id,
                MerchantBearingRejectRequest body,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                await mediator.Send(new RejectMerchantBearingCommand
                {
                    MerchantBearingId = id,
                    ReviewedBy = currentUser.UserId ?? Guid.Empty,
                    Reason = body.Reason ?? string.Empty
                });
                return ApiResponseHelper.Ok("已驳回", httpContext);
            })
            .WithName("RejectMerchantBearing")
            .WithSummary("驳回商家商品")
            .WithDescription("驳回商家商品审核（附理由）")
            .RequirePermission("merchant.verify");

            // ============ 替代品治理（v2.16.0）：平台建标准、审关系 ============

            /// <summary>替代关系管理列表（平台）</summary>
            group.MapGet("/interchanges", async (
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] string? keyword = null,
                // 改动说明（v2.17.0 字典化）：去掉审核状态过滤参数（替代关系审核状态机已取消）
                [FromQuery] string? createdFrom = null,
                [FromQuery] string? createdTo = null,
                [FromQuery] bool? internetSource = null,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20) =>
            {
                // 改动说明（v1.50.0 下钻筛选）：创建日期按 yyyy-MM-dd 解析（业务日历），处理器换算为 UTC 区间
                DateOnly? incFrom = DateOnly.TryParse(createdFrom, CultureInfo.InvariantCulture, DateTimeStyles.None, out var icf) ? icf : null;
                DateOnly? incTo = DateOnly.TryParse(createdTo, CultureInfo.InvariantCulture, DateTimeStyles.None, out var ict) ? ict : null;
                var result = await mediator.Send(new GetInterchangesQuery
                {
                    Keyword = keyword,
                    CreatedFrom = incFrom,
                    CreatedTo = incTo,
                    InternetSource = internetSource,
                    Page = page,
                    PageSize = pageSize
                });
                return ApiResponseHelper.Paged(result.Items.ToList(), result.TotalCount, result.Page, result.PageSize, httpContext);
            })
            .WithName("GetInterchanges")
            .WithSummary("替代关系列表")
            .WithDescription("平台管理替代关系列表（可按关键字/创建时间/来源过滤）")
            // 改动说明（v2.17.0 替代品字典化）：替代品属主数据，权限由 merchant.verify 改为 interchange.view
            .RequirePermission("interchange.view");

            // 改动说明（v2.17.0 替代品字典化）：删除 /interchanges/pending 端点——
            //   商家候选流取消，不再有"待审核队列"

            /// <summary>平台新建替代关系（直接生效）</summary>
            group.MapPost("/interchanges", async (
                CreateInterchangeRequest body,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var id = await mediator.Send(new CreateInterchangeCommand
                {
                    SourceBearingId = body.SourceBearingId,
                    TargetBearingId = body.TargetBearingId,
                    InterchangeType = body.InterchangeType,
                    Confidence = body.Confidence ?? 80,
                    Source = body.Source,
                    Remarks = body.Remarks,
                    IsBidirectional = body.IsBidirectional ?? true
                });
                return ApiResponseHelper.Ok(new { id }, "已创建", httpContext);
            })
            .WithName("CreateInterchange")
            .WithSummary("新建替代关系")
            .WithDescription("平台自建替代关系（直接生效、公域可见）")
            // 改动说明（v2.17.0 替代品字典化）：写操作权限改为 interchange.manage
            .RequirePermission("interchange.manage");

            /// <summary>平台编辑替代关系</summary>
            group.MapPut("/interchanges/{id:guid}", async (
                Guid id,
                UpdateInterchangeRequest body,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                await mediator.Send(new UpdateInterchangeCommand
                {
                    Id = id,
                    InterchangeType = body.InterchangeType,
                    Confidence = body.Confidence ?? 80,
                    Source = body.Source,
                    Remarks = body.Remarks,
                    IsBidirectional = body.IsBidirectional ?? true
                });
                return ApiResponseHelper.Ok("已更新", httpContext);
            })
            .WithName("UpdateInterchange")
            .WithSummary("编辑替代关系")
            .WithDescription("平台编辑替代关系的类型/可信度/来源/备注/双向")
            // 改动说明（v2.17.0 替代品字典化）：写操作权限改为 interchange.manage
            .RequirePermission("interchange.manage");

            /// <summary>平台删除替代关系（物理删除）</summary>
            group.MapDelete("/interchanges/{id:guid}", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                await mediator.Send(new DeleteInterchangeCommand { Id = id });
                return ApiResponseHelper.Ok("已删除", httpContext);
            })
            .WithName("DeleteInterchange")
            .WithSummary("删除替代关系")
            .WithDescription("平台删除替代关系（物理删除，唯一键释放）")
            // 改动说明（v2.17.0 替代品字典化）：写操作权限改为 interchange.manage
            .RequirePermission("interchange.manage");

            // 改动说明（v2.17.0 替代品字典化）：删除"审核通过/审核驳回"两个端点——
            //   替代关系不再有候选待审，平台在"替代品管理"页直接维护

            // ============ 4.2 轴承管理 ============

            /// <summary>
            /// 轴承列表（管理端，支持 includeDeleted）
            /// </summary>
            group.MapGet("/bearings", async (
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] string? keyword = null,
                [FromQuery] bool? includeDeleted = null,
                [FromQuery] string? createdFrom = null,
                [FromQuery] string? createdTo = null,
                [FromQuery] bool? internetSource = null,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20) =>
            {
                // 改动说明（v1.50.0 下钻筛选）：创建日期按 yyyy-MM-dd 解析（业务日历），处理器换算为 UTC 区间
                DateOnly? bFrom = DateOnly.TryParse(createdFrom, CultureInfo.InvariantCulture, DateTimeStyles.None, out var bf) ? bf : null;
                DateOnly? bTo = DateOnly.TryParse(createdTo, CultureInfo.InvariantCulture, DateTimeStyles.None, out var bt) ? bt : null;
                var query = new SearchBearingsQuery
                {
                    Keyword = keyword,
                    IncludeDeleted = includeDeleted,
                    CreatedFrom = bFrom,
                    CreatedTo = bTo,
                    InternetSource = internetSource,
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
            .WithName("AdminGetBearings")
            .WithSummary("获取轴承列表")
            .WithDescription("管理端轴承列表，支持搜索和显示已删除")
            .RequirePermission("bearing.view");

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
            .RequirePermission("bearing.create");

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
            .RequirePermission("bearing.edit");

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
            .RequirePermission("bearing.delete");

            /// <summary>
            /// 恢复轴承
            /// </summary>
            group.MapPut("/bearings/{id:guid}/restore", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var command = new RestoreBearingCommand(id);
                await mediator.Send(command);
                return ApiResponseHelper.Ok("轴承恢复成功", httpContext);
            })
            .WithName("AdminRestoreBearing")
            .WithSummary("恢复轴承")
            .WithDescription("恢复已删除的轴承")
            .RequirePermission("data.restore");

            /// <summary>
            /// 彻底删除轴承
            /// </summary>
            group.MapDelete("/bearings/{id:guid}/hard", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var command = new HardDeleteBearingCommand(id);
                await mediator.Send(command);
                return ApiResponseHelper.Ok("轴承已彻底删除", httpContext);
            })
            .WithName("AdminHardDeleteBearing")
            .WithSummary("彻底删除轴承")
            .WithDescription("物理删除轴承，不可恢复")
            .RequirePermission("data.harddelete");

            // ============ 4.3 商家管理 ============

            /// <summary>
            /// 商家详情（按 ID 查询，供后台关联展示）
            /// </summary>
            group.MapGet("/merchants/{id:guid}", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var query = new GetMerchantQuery { Id = id, IsAuthenticated = true };
                var result = await mediator.Send(query);
                return result == null
                    ? ApiResponseHelper.NotFound("商家不存在", httpContext)
                    : ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("AdminGetMerchantById")
            .WithSummary("获取商家详情")
            .WithDescription("按 ID 获取商家详细信息")
            .RequirePermission("merchant.manage");

            /// <summary>
            /// 商户证照材料列表（v2.7.0 新增，审批抽屉"申请材料"区数据源）
            /// </summary>
            group.MapGet("/merchants/{id:guid}/documents", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var result = await mediator.Send(new GetMerchantDocumentsQuery { MerchantId = id });
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetMerchantDocuments")
            .WithSummary("获取商户证照材料")
            .WithDescription("按商户返回全部证照材料（含状态），供入驻审批抽屉与认证口径核对")
            .RequirePermission("merchant.verify");

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
            /// 恢复商家
            /// </summary>
            group.MapPut("/merchants/{id:guid}/restore", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var command = new RestoreMerchantCommand(id);
                await mediator.Send(command);
                return ApiResponseHelper.Ok("商家恢复成功", httpContext);
            })
            .WithName("AdminRestoreMerchant")
            .WithSummary("恢复商家")
            .WithDescription("恢复已删除的商家")
            .RequirePermission("data.restore");

            /// <summary>
            /// 彻底删除商家
            /// </summary>
            group.MapDelete("/merchants/{id:guid}/hard", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var command = new HardDeleteMerchantCommand(id);
                await mediator.Send(command);
                return ApiResponseHelper.Ok("商家已彻底删除", httpContext);
            })
            .WithName("AdminHardDeleteMerchant")
            .WithSummary("彻底删除商家")
            .WithDescription("物理删除商家，不可恢复")
            .RequirePermission("data.harddelete");

            /// <summary>
            /// 强制解除商户归属（v2.17.0）：管理员跑路/僵尸无主/违规商户的平台侧处置——
            /// 清场回公海（商品/证照/邀请/纠错全清、联系方式隐私止血），存在性交互联网管线裁判；
            /// self/提名新建商户无公海数据会被拒绝（应走删除）。
            /// </summary>
            group.MapPost("/merchants/{id:guid}/detach", async (
                Guid id,
                [FromQuery] string? reason,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var result = await mediator.Send(new DetachMerchantCommand(id, reason));


                return ApiResponseHelper.Ok($"已解除商户「{result.MerchantName}」归属并退回公开信息池", httpContext);
            })
            .WithName("AdminDetachMerchant")
            .WithSummary("强制解除商户归属")
            .WithDescription("平台侧解除认领商户的归属：清场回公海可再认领，资料随互联网数据自然更新")
            .RequirePermission("merchant.detach");

            /// <summary>
            /// 审核通过入驻申请（Pending -> Active，使商户生效）
            /// 与认证（verify）分离：approve 决定商户是否生效，verify 决定认证等级
            /// </summary>
            group.MapPost("/merchants/{id:guid}/approve", async (
                Guid id,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var command = new ApproveMerchantCommand(id, currentUser.UserId.Value.ToString());
                await mediator.Send(command);
                return ApiResponseHelper.Ok("入驻审核通过，商户已生效", httpContext);
            })
            .WithName("ApproveMerchant")
            .WithSummary("审核通过入驻申请")
            .WithDescription("审核通过入驻申请，商户由待审核转为生效状态")
            .RequirePermission("merchant.verify");

            /// <summary>
            /// 平台指定商户成员（G4 兜底：商户无在职管理员等异常时平台救援）
            /// 已存在成员行则恢复/改角色，否则新建；复用 merchant.verify 权限（平台审核组）
            /// </summary>
            group.MapPost("/merchants/{id:guid}/members", async (
                Guid id,
                AddPlatformMemberRequest request,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var command = new AssignMerchantMemberCommand(id, request.UserId, request.Role ?? MerchantMember.RoleMerchantAdmin);
                var memberId = await mediator.Send(command);
                return ApiResponseHelper.Ok(new { memberId, message = "平台成员操作完成" }, httpContext: httpContext);
            })
            .WithName("AssignMerchantMemberByPlatform")
            .WithSummary("平台指定商户成员")
            .WithDescription("平台在商户无管理员等异常场景下直接指定/恢复成员（兜底通道）")
            .RequirePermission("merchant.verify");

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

                var command = new VerifyMerchantCommand(id, currentUser.UserId.Value.ToString());
                await mediator.Send(command);
                return ApiResponseHelper.Ok("商家认证成功，已清除互联网来源数据", httpContext);
            })
            .WithName("VerifyMerchant")
            .WithSummary("认证商家")
            .WithDescription("商家认证审核，通过后将清除该商家的互联网来源数据")
            .RequirePermission("merchant.verify");

            /// <summary>
            /// 拒绝商家认证
            /// </summary>
            group.MapPost("/merchants/{id:guid}/reject", async (
                Guid id,
                RejectMerchantRequest request,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var command = new RejectMerchantCommand(id, request.Reason, currentUser.UserId.Value);
                await mediator.Send(command);
                return ApiResponseHelper.Ok("已拒绝商家认证", httpContext);
            })
            .WithName("RejectMerchant")
            .WithSummary("拒绝商家认证")
            .WithDescription("拒绝商家认证申请，需填写拒绝理由")
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
                [FromQuery] bool? includeDeleted = null,
                [FromQuery] int? status = null,
                [FromQuery] bool? excludeInternet = null,
                [FromQuery] string? registeredFrom = null,
                [FromQuery] string? registeredTo = null,
                [FromQuery] string? onboardedFrom = null,
                [FromQuery] string? onboardedTo = null,
                [FromQuery] bool? internetSource = null,
                [FromQuery] string? sortBy = null,
                [FromQuery] string? sortOrder = null,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20) =>
            {
                // 改动说明（v1.50.0 商家注册/入驻时间与来源筛选）：日期按 yyyy-MM-dd 解析（业务日历），
                //   由查询处理器换算为 UTC 区间；internetSource 为来源拆分下钻用（true=仅互联网）
                DateOnly? regFrom = DateOnly.TryParse(registeredFrom, CultureInfo.InvariantCulture, DateTimeStyles.None, out var rf) ? rf : null;
                DateOnly? regTo = DateOnly.TryParse(registeredTo, CultureInfo.InvariantCulture, DateTimeStyles.None, out var rt) ? rt : null;
                DateOnly? onbFrom = DateOnly.TryParse(onboardedFrom, CultureInfo.InvariantCulture, DateTimeStyles.None, out var of) ? of : null;
                DateOnly? onbTo = DateOnly.TryParse(onboardedTo, CultureInfo.InvariantCulture, DateTimeStyles.None, out var ot) ? ot : null;
                var query = new SearchMerchantsQuery
                {
                    Keyword = keyword,
                    City = city,
                    Type = type.HasValue ? (MerchantType?)type : null,
                    Status = status.HasValue ? (MerchantStatus?)status : null,
                    VerifiedOnly = verifiedOnly,
                    IncludeDeleted = includeDeleted,
                    ExcludeInternet = excludeInternet,
                    RegisteredFrom = regFrom,
                    RegisteredTo = regTo,
                    OnboardedFrom = onbFrom,
                    OnboardedTo = onbTo,
                    InternetSource = internetSource,
                    SortBy = sortBy,
                    SortOrder = sortOrder,
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
            /// 品牌列表（管理端，支持 includeDeleted）
            /// </summary>
            group.MapGet("/brands", async (
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] bool? includeDeleted = null) =>
            {
                var query = new GetAllBrandsQuery { IncludeDeleted = includeDeleted };
                var result = await mediator.Send(query);
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("AdminGetBrands")
            .WithSummary("获取品牌列表")
            .WithDescription("管理端品牌列表，支持显示已删除")
            .RequirePermission("brand.view");

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
            .RequirePermission("brand.create");

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
            .RequirePermission("brand.edit");

            /// <summary>
            /// 删除品牌（软删除）
            /// </summary>
            group.MapDelete("/brands/{id:guid}", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var command = new DeleteBrandCommand(id);
                await mediator.Send(command);
                return ApiResponseHelper.Ok("品牌删除成功", httpContext);
            })
            .WithName("AdminDeleteBrand")
            .WithSummary("删除品牌")
            .WithDescription("删除品牌（软删除）")
            .RequirePermission("brand.delete");

            /// <summary>
            /// 恢复品牌
            /// </summary>
            group.MapPut("/brands/{id:guid}/restore", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var command = new RestoreBrandCommand(id);
                await mediator.Send(command);
                return ApiResponseHelper.Ok("品牌恢复成功", httpContext);
            })
            .WithName("AdminRestoreBrand")
            .WithSummary("恢复品牌")
            .WithDescription("恢复已删除的品牌")
            .RequirePermission("data.restore");

            /// <summary>
            /// 彻底删除品牌
            /// </summary>
            group.MapDelete("/brands/{id:guid}/hard", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var command = new HardDeleteBrandCommand(id);
                await mediator.Send(command);
                return ApiResponseHelper.Ok("品牌已彻底删除", httpContext);
            })
            .WithName("AdminHardDeleteBrand")
            .WithSummary("彻底删除品牌")
            .WithDescription("物理删除品牌，不可恢复")
            .RequirePermission("data.harddelete");

            /// <summary>
            /// 轴承类型列表（管理端，支持 includeDeleted）
            /// </summary>
            group.MapGet("/bearing-types", async (
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] bool? includeDeleted = null) =>
            {
                var query = new GetAllBearingTypesQuery { IncludeDeleted = includeDeleted };
                var result = await mediator.Send(query);
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("AdminGetBearingTypes")
            .WithSummary("获取轴承类型列表")
            .WithDescription("管理端类型列表，支持显示已删除")
            .RequirePermission("type.view");

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
            .RequirePermission("type.create");

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
            .RequirePermission("type.edit");

            /// <summary>
            /// 删除轴承类型（软删除）
            /// </summary>
            group.MapDelete("/bearing-types/{id:guid}", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var command = new DeleteBearingTypeCommand(id);
                await mediator.Send(command);
                return ApiResponseHelper.Ok("轴承类型删除成功", httpContext);
            })
            .WithName("AdminDeleteBearingType")
            .WithSummary("删除轴承类型")
            .WithDescription("删除轴承类型（软删除）")
            .RequirePermission("type.delete");

            /// <summary>
            /// 恢复轴承类型
            /// </summary>
            group.MapPut("/bearing-types/{id:guid}/restore", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var command = new RestoreBearingTypeCommand(id);
                await mediator.Send(command);
                return ApiResponseHelper.Ok("轴承类型恢复成功", httpContext);
            })
            .WithName("AdminRestoreBearingType")
            .WithSummary("恢复轴承类型")
            .WithDescription("恢复已删除的轴承类型")
            .RequirePermission("data.restore");

            /// <summary>
            /// 彻底删除轴承类型
            /// </summary>
            group.MapDelete("/bearing-types/{id:guid}/hard", async (
                Guid id,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var command = new HardDeleteBearingTypeCommand(id);
                await mediator.Send(command);
                return ApiResponseHelper.Ok("轴承类型已彻底删除", httpContext);
            })
            .WithName("AdminHardDeleteBearingType")
            .WithSummary("彻底删除轴承类型")
            .WithDescription("物理删除轴承类型，不可恢复")
            .RequirePermission("data.harddelete");

            // ============ 4.6 系统配置 ============

            /// <summary>
            /// 获取系统配置
            /// </summary>
            group.MapGet("/config", async (
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] string? groupFilter = null) =>
            {
                var query = new GetSystemConfigsQuery
                {
                    Group = groupFilter
                };
                var result = await mediator.Send(query);
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetSystemConfigs")
            .WithSummary("获取系统配置")
            .WithDescription("获取系统配置列表")
            .RequirePermission("system.view");

            /// <summary>
            /// 更新系统配置
            /// </summary>
            group.MapPut("/config/{key}", async (
                string key,
                UpdateSystemConfigCommand command,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                [FromServices] IPriceConfigProvider priceConfigProvider,
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

                // 改动说明：价格类配置变更后失效进程内缓存。
                //           失效动作必须放在 mediator.Send 返回之后——事务由 UnitOfWorkBehavior
                //           在 Handler 执行完毕后才提交，若放在 Handler 内会早于提交，
                //           并发请求在提交前重新加载到的仍是旧值并缓存 5 分钟，
                //           恰好复现"页面已显示新值、API 仍按旧值写库"的不一致窗口
                if (key.StartsWith("Price.", StringComparison.OrdinalIgnoreCase))
                    priceConfigProvider.Invalidate();

                return ApiResponseHelper.Ok("配置更新成功", httpContext);
            })
            .WithName("UpdateSystemConfig")
            .WithSummary("更新系统配置")
            .WithDescription("更新指定配置")
            .RequirePermission("system.manage");

            /// <summary>
            /// 刷新限流配置缓存
            /// </summary>
            group.MapPost("/cache/refresh-rate-limit", async (
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
            .RequirePermission("system.manage");

            /// <summary>
            /// 获取价格配置
            /// </summary>
            /// 改动说明：原实现走 GetPriceConfigQuery（MediatR 查询，每次拉全表且无缓存），
            ///           与商品创建链路使用的 IPriceConfigProvider 构成两套读取路径，
            ///           且二者对空值的兜底处理不一致。此处统一改用带缓存的提供器，消除行为分叉
            group.MapGet("/config/price", async (
                [FromServices] IPriceConfigProvider priceConfigProvider,
                HttpContext httpContext) =>
            {
                var result = await priceConfigProvider.GetAsync(httpContext.RequestAborted);
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("GetPriceConfig")
            .WithSummary("获取价格配置")
            .WithDescription("获取价格相关配置")
            .RequirePermission("system.view");

            /// <summary>
            /// 获取待审核的证照材料队列（v2.7.0：入驻后换证/补授权书等变更材料；
            ///   随入驻申请提交的材料在入驻审批抽屉内级联审，不入本队列）
            /// </summary>
            group.MapGet("/documents/pending", async (
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20) =>
            {
                var query = new GetPendingDocumentsQuery
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
            .WithName("GetPendingDocuments")
            .WithSummary("获取待审核证照材料")
            .WithDescription("获取所有待审核的商户证照材料（换证/补授权书等入驻后变更）")
            .RequirePermission("merchant.verify");

            /// <summary>
            /// 审核通过证照材料（通过后不自动认证——认证改走 verify 端点按材料矩阵口径校验，
            ///   v2.7.0 移除旧"执照通过即认证"耦合）
            /// </summary>
            group.MapPost("/documents/{id:guid}/approve", async (
                Guid id,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var command = new ApproveDocumentCommand
                {
                    VerificationId = id,
                    ReviewedBy = currentUser.UserId.Value
                };
                await mediator.Send(command);
                return ApiResponseHelper.Ok("材料已审核通过", httpContext);
            })
            .WithName("ApproveDocument")
            .WithSummary("审核通过证照材料")
            .WithDescription("通过商户证照材料变更审核（认证标由 verify 端点按材料矩阵单独判定）")
            .RequirePermission("merchant.verify");

            /// <summary>
            /// 审核拒绝证照材料
            /// </summary>
            group.MapPost("/documents/{id:guid}/reject", async (
                Guid id,
                RejectDocumentCommand command,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                command = command with
                {
                    VerificationId = id,
                    ReviewedBy = currentUser.UserId.Value
                };
                await mediator.Send(command);
                return ApiResponseHelper.Ok("已拒绝", httpContext);
            })
            .WithName("RejectDocument")
            .WithSummary("审核拒绝证照材料")
            .WithDescription("拒绝商户证照材料变更审核，填写拒绝理由")
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
            .RequirePermission("role.manage");

            /// <summary>
            /// 创建角色
            /// </summary>
            group.MapPost("/roles", async (
                CreateRoleCommand command,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                var id = await mediator.Send(command);
                return ApiResponseHelper.Ok(new { id }, "角色创建成功", httpContext);
            })
            .WithName("CreateRole")
            .WithSummary("创建角色")
            .WithDescription("创建新角色")
            .RequirePermission("role.manage");

            /// <summary>
            /// 更新角色
            /// </summary>
            group.MapPut("/roles/{id:guid}", async (
                Guid id,
                UpdateRoleCommand command,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                command = command with { Id = id };
                await mediator.Send(command);
                return ApiResponseHelper.Ok("角色更新成功", httpContext);
            })
            .WithName("UpdateRole")
            .WithSummary("更新角色")
            .WithDescription("更新角色信息")
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
            .RequirePermission("role.manage");

            /// <summary>
            /// 为角色分配权限
            /// </summary>
            group.MapPost("/roles/{id:guid}/permissions", async (
                Guid id,
                AssignPermissionsToRoleCommand command,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                command = command with { RoleId = id };
                await mediator.Send(command);
                return ApiResponseHelper.Ok("权限分配成功", httpContext);
            })
            .WithName("AssignPermissionsToRole")
            .WithSummary("分配权限")
            .WithDescription("为角色分配权限")
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
                [FromQuery] string? groupFilter = null) =>
            {
                var query = new GetPermissionsQuery
                {
                    Page = page,
                    PageSize = pageSize,
                    Group = groupFilter
                };
                var result = await mediator.Send(query);
                return ApiResponseHelper.Paged(result.Items.ToList(), result.TotalCount, result.Page, result.PageSize, httpContext);
            })
            .WithName("GetPermissions")
            .WithSummary("获取权限列表")
            .WithDescription("分页获取所有权限列表")
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
            .RequirePermission("role.manage");

            /// <summary>
            /// 获取用户的角色列表
            /// </summary>
            group.MapGet("/users/{userId:guid}/roles", async (
                Guid userId,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var query = new GetUserRolesQuery { UserId = userId };
                var result = await mediator.Send(query);
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("AdminGetUserRoles")
            .WithSummary("获取用户角色")
            .WithDescription("获取用户的角色列表")
            .RequirePermission("user.assign");

            /// <summary>
            /// 获取用户的权限列表
            /// </summary>
            group.MapGet("/users/{userId:guid}/permissions", async (
                Guid userId,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var query = new GetUserPermissionsQuery { UserId = userId };
                var result = await mediator.Send(query);
                return ApiResponseHelper.Ok(result, httpContext: httpContext);
            })
            .WithName("AdminGetUserPermissions")
            .WithSummary("获取用户权限")
            .WithDescription("获取用户的权限列表")
            .RequirePermission("user.assign");

            /// <summary>
            /// 按认证主体（Identity sub）获取平台角色（v1.31.0）
            /// </summary>
            group.MapGet("/users/by-auth/{authUserId:guid}/roles", async (
                Guid authUserId,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                // 改动说明（v1.31.0）：Admin 用户页以 Identity 用户 ID（=sub=AuthUserId）为键管理
                //   平台角色，API 业务用户 ID 与之不同，需先按 AuthUserId 解析；未 JIT 的用户返回空清单
                var user = await mediator.Send(new GetUserByAuthIdQuery { AuthUserId = authUserId.ToString() });
                if (user == null)
                    return ApiResponseHelper.Ok(new List<string>(), httpContext: httpContext);

                var roles = await mediator.Send(new GetUserRolesQuery { UserId = user.Id });
                return ApiResponseHelper.Ok(roles, httpContext: httpContext);
            })
            .WithName("AdminGetUserRolesByAuth")
            .WithSummary("按认证主体获取角色")
            .WithDescription("以 Identity sub 为键查询平台角色，供 Admin 用户页角色分配")
            .RequirePermission("user.assign");

            /// <summary>
            /// 按认证主体分配平台角色（v1.31.0）
            /// </summary>
            group.MapPost("/users/by-auth/{authUserId:guid}/roles", async (
                Guid authUserId,
                AssignRoleToUserCommand command,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var user = await mediator.Send(new GetUserByAuthIdQuery { AuthUserId = authUserId.ToString() });
                if (user == null)
                    return ApiResponseHelper.NotFound("该用户尚未登录过业务系统，无平台角色记录", httpContext);

                await mediator.Send(command with { UserId = user.Id });
                return ApiResponseHelper.Ok("角色分配成功", httpContext);
            })
            .WithName("AdminAssignRoleByAuth")
            .WithSummary("按认证主体分配角色")
            .WithDescription("以 Identity sub 为键分配平台角色")
            .RequirePermission("user.assign");

            /// <summary>
            /// 按认证主体移除平台角色（v1.31.0）
            /// </summary>
            group.MapDelete("/users/by-auth/{authUserId:guid}/roles/{roleName}", async (
                Guid authUserId,
                string roleName,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var user = await mediator.Send(new GetUserByAuthIdQuery { AuthUserId = authUserId.ToString() });
                if (user == null)
                    return ApiResponseHelper.NotFound("该用户尚未登录过业务系统，无平台角色记录", httpContext);

                await mediator.Send(new RemoveRoleFromUserCommand { UserId = user.Id, RoleName = roleName });
                return ApiResponseHelper.Ok("角色移除成功", httpContext);
            })
            .WithName("AdminRemoveRoleByAuth")
            .WithSummary("按认证主体移除角色")
            .WithDescription("以 Identity sub 为键移除平台角色")
            .RequirePermission("user.assign");

            /// <summary>
            /// 批量获取全部用户的平台角色映射（v1.38.0）
            /// 改动说明：Admin 用户列表角色列需要按 Identity sub 合并 API 侧平台角色，
            /// 逐行调 by-auth 会 N+1；后台角色人数有限，一次全量返回字典 sub→roles[]
            /// </summary>
            group.MapGet("/users/platform-roles", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var map = await mediator.Send(new GetAllPlatformRolesQuery(), cancellationToken);
                return ApiResponseHelper.Ok(map, httpContext: httpContext);
            })
            .WithName("AdminGetAllPlatformRoles")
            .WithSummary("批量获取平台角色映射")
            .WithDescription("返回所有挂了平台角色的用户 sub→角色名列表字典，供 Admin 用户列表合并展示")
            .RequirePermission("user.view");

            /// <summary>
            /// 预置业务用户并挂平台角色（v1.38.0）
            /// 改动说明：后台新建 Identity 账号后业务库无 User 行（JIT 制），平台角色分配必 404；
            /// 本端点 find-or-create 建行 + 批量授权，供 Admin"新建后台用户"弹窗建号即分配
            /// </summary>
            group.MapPost("/users/provision", async (
                ProvisionUserCommand command,
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMediator mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var id = await mediator.Send(command, cancellationToken);
                return ApiResponseHelper.Ok(new { id }, "用户预置成功", httpContext);
            })
            .WithName("AdminProvisionUser")
            .WithSummary("预置业务用户并挂角色")
            .WithDescription("按 Identity sub find-or-create 业务用户行并批量授予平台角色（幂等）")
            .RequirePermission("user.assign");
        }
    }

    /// <summary>
    /// 平台指定商户成员请求体（G4 兜底通道）
    /// </summary>
    public record AddPlatformMemberRequest(Guid UserId, string? Role);

    /// <summary>
    /// 商家商品驳回请求体（v2.15.0 商品审核）
    /// </summary>
    public record MerchantBearingRejectRequest(string? Reason);
}
