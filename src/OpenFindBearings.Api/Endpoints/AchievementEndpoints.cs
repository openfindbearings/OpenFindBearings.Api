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
                    imageKey = d.ImageKey,
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
                    req.RewardPoints, req.TitleReward, req.Enabled, req.ImageKey);
                repo.UpdateDefinition(def);
                // 端点直连仓储不走 MediatR 管道，必须显式提交（与积分规则 PUT 同模式）
                await unitOfWork.SaveChangesAsync(httpContext.RequestAborted);
                return ApiResponseHelper.Ok(new { id = def.Id }, httpContext: httpContext);
            })
            .WithName("AdminUpdateAchievement")
            .WithSummary("编辑成就")
            .RequirePermission("system.manage");

            /// <summary>
            /// 上传/替换勋章图（v2.6.0 勋章图片管线）：仅 jpg/png/webp ≤2MB。
            /// 可替换语义：先删旧键再传新键（新键含时间戳，键变 URL 变，前端无缓存残留），
            /// 成功后更新实体 ImageKey 回落库——反复上传即反复替换，不留孤儿对象
            /// </summary>
            admin.MapPost("/{id:guid}/image", async (
                Guid id,
                IFormFile file,
                [FromServices] IAchievementRepository repo,
                [FromServices] IObjectStorageService storage,
                [FromServices] OpenFindBearings.Application.Shared.Interfaces.IUnitOfWork unitOfWork,
                HttpContext httpContext) =>
            {
                var def = await repo.GetDefinitionByIdAsync(id);
                if (def == null)
                    return ApiResponseHelper.NotFound(httpContext: httpContext);

                if (file == null || file.Length == 0)
                    return ApiResponseHelper.BadRequest("请上传文件", httpContext: httpContext);

                // 改动说明（v2.6.0）：白名单与大小限制沿用头像端点口径，扩展名缺失回退 MIME 推断
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var fileExtension = FileUploadHelper.GetSafeExtension(file);
                if (!allowedExtensions.Contains(fileExtension))
                    return ApiResponseHelper.BadRequest("只支持 JPG、PNG、WEBP 格式", httpContext: httpContext);
                if (file.Length > 2 * 1024 * 1024)
                    return ApiResponseHelper.BadRequest("图片文件不能超过2MB", httpContext: httpContext);

                // 可替换关键：先落新键名（时间戳版本），再存对象、再删旧键——顺序保证任何一步失败都不丢图。
                // ImageKey 存 URL 形态（带前导 /，与 LogoUrl/Avatar 一致，usableImage 只渲染 / 开头）；
                // 删旧时对 URL 形态 TrimStart('/') 还原裸 key 供对象存储删除
                var newKey = $"uploads/achievements/{def.Key}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";
                using var buffer = new MemoryStream();
                await file.CopyToAsync(buffer);
                var fileUrl = await storage.UploadAsync(newKey, buffer.ToArray(),
                    FileUploadHelper.ContentTypeFromExtension(fileExtension), httpContext.RequestAborted);
                if (!string.IsNullOrEmpty(def.ImageKey))
                    await storage.DeleteAsync(def.ImageKey.TrimStart('/'), httpContext.RequestAborted);
                def.SetImageKey(fileUrl);
                repo.UpdateDefinition(def);
                // 端点直连仓储不走 MediatR 管道，必须显式提交（与 PUT 同模式）
                await unitOfWork.SaveChangesAsync(httpContext.RequestAborted);
                return ApiResponseHelper.Ok(new { url = fileUrl, imageKey = fileUrl }, httpContext: httpContext);
            })
            .WithName("AdminUploadAchievementImage")
            .WithSummary("上传/替换勋章图")
            .WithDescription("上传勋章图片（jpg/png/webp ≤2MB）；重复上传即替换旧图")
            .DisableAntiforgery()
            .RequirePermission("system.manage");
        }
    }

    /// <summary>Admin 编辑成就请求体</summary>
    public record UpdateAchievementRequest(
        string Name, string Description, int ProgressTarget, int MetaPoints,
        int RewardPoints, string? TitleReward, bool Enabled, string? ImageKey = null);
}
