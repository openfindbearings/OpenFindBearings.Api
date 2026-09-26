using Microsoft.AspNetCore.Mvc;
using OpenFindBearings.Api.Extensions;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Api.Services;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Api.Endpoints
{
    /// <summary>
    /// 成就接口（v2.1.0 成就子系统）：个人成就墙/我的徽章排 + 商户徽章排（B2B 信任信号）
    /// </summary>
    public static class AchievementEndpoints
    {
        /// <summary>
        /// 映射成就端点
        /// </summary>
        public static void MapAchievementEndpoints(this IEndpointRouteBuilder app)
        {
            // ============ 个人端（登录用户） ============
            var group = app.MapGroup("/api").RequireAuthorization();

            /// <summary>
            /// 个人成就墙：全目录+本人进度（隐藏成就未解锁不显示），含成就点合计与当前称号
            /// </summary>
            group.MapGet("/achievements/wall", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IAchievementService achievementService,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);
                var view = await achievementService.GetWallAsync(currentUser.UserId.Value);
                return ApiResponseHelper.Ok(view, httpContext: httpContext);
            })
            .WithName("GetAchievementWall")
            .WithSummary("个人成就墙")
            .WithDescription("全成就目录+本人进度+成就点合计+当前称号");

            /// <summary>
            /// 我的已解锁徽章排（个人资料页横向徽章条数据源）
            /// </summary>
            group.MapGet("/me/achievements", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IAchievementService achievementService,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);
                var view = await achievementService.GetMyAsync(currentUser.UserId.Value);
                return ApiResponseHelper.Ok(view, httpContext: httpContext);
            })
            .WithName("GetMyAchievements")
            .WithSummary("我的已解锁徽章")
            .WithDescription("个人资料页徽章排数据源");

            // ============ 商户徽章排（公开，商户详情/卡片信任信号） ============
            app.MapGet("/api/merchants/{id:guid}/achievements", async (
                Guid id,
                [FromServices] IAchievementService achievementService,
                HttpContext httpContext) =>
            {
                var view = await achievementService.GetMerchantAsync(id);
                return ApiResponseHelper.Ok(view, httpContext: httpContext);
            })
            .WithName("GetMerchantAchievements")
            .WithSummary("商户徽章排")
            .WithDescription("商户详情/卡片展示的商户成就徽章（B2B 信任信号）");

            // ============ Admin 端（成就目录管理，复用 system.manage 权限免新增迁移） ============
            var admin = app.MapGroup("/api/admin/achievements").RequireAuthorization();

            /// <summary>成就目录全量（含停用，Admin 管理页数据源）</summary>
            admin.MapGet("", async (
                [FromServices] IAchievementRepository repo,
                HttpContext httpContext) =>
            {
                var defs = await repo.GetAllDefinitionsAsync();
                return ApiResponseHelper.Ok(defs.Select(d => new
                {
                    id = d.Id,
                    key = d.Key,
                    name = d.Name,
                    description = d.Description,
                    icon = d.Icon,
                    scope = (int)d.Scope,
                    category = d.Category,
                    metricKey = d.MetricKey,
                    progressTarget = d.ProgressTarget,
                    metaPoints = d.MetaPoints,
                    rewardPoints = d.RewardPoints,
                    titleReward = d.TitleReward,
                    rare = d.Rare,
                    hidden = d.Hidden,
                    enabled = d.Enabled
                }), httpContext: httpContext);
            })
            .WithName("AdminGetAchievements")
            .WithSummary("成就目录全量")
            .RequirePermission("system.manage");

            /// <summary>编辑成就（分值/阈值/称号/启停；Key/Metric/Scope 不可改）</summary>
            admin.MapPut("/{id:guid}", async (
                Guid id,
                [FromBody] UpdateAchievementRequest req,
                [FromServices] IAchievementRepository repo,
                [FromServices] OpenFindBearings.Application.Shared.Interfaces.IUnitOfWork unitOfWork,
                HttpContext httpContext) =>
            {
                var def = await repo.GetDefinitionByIdAsync(id);
                if (def == null)
                    return ApiResponseHelper.NotFound(httpContext: httpContext);
                def.Update(req.Name, req.Description, req.ProgressTarget, req.MetaPoints,
                    req.RewardPoints, req.TitleReward, req.Enabled);
                repo.UpdateDefinition(def);
                // 端点直连仓储不走 MediatR 管道，必须显式提交（与积分规则 PUT 同模式）
                await unitOfWork.SaveChangesAsync(httpContext.RequestAborted);
                return ApiResponseHelper.Ok(new { id = def.Id }, httpContext: httpContext);
            })
            .WithName("AdminUpdateAchievement")
            .WithSummary("编辑成就")
            .RequirePermission("system.manage");
        }
    }

    /// <summary>Admin 编辑成就请求体</summary>
    public record UpdateAchievementRequest(
        string Name, string Description, int ProgressTarget, int MetaPoints,
        int RewardPoints, string? TitleReward, bool Enabled);
}
