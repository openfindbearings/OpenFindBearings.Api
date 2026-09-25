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
    /// 积分接口（v1.32.0 积分底座）：个人端（账户/签到/流水）+ Admin 端（规则配置，分值调整实时生效不发版）
    /// </summary>
    public static class PointsEndpoints
    {
        /// <summary>
        /// 映射积分端点
        /// </summary>
        public static void MapPointsEndpoints(this IEndpointRouteBuilder app)
        {
            // ============ 个人端（登录用户） ============
            var group = app.MapGroup("/api/points").RequireAuthorization();

            /// <summary>
            /// 积分账户概览：余额/累计/今日签到状态/连续天数（我的页积分卡与签到按钮数据源）
            /// </summary>
            group.MapGet("/account", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IPointAccountRepository accountRepository,
                [FromServices] IPointTransactionRepository transactionRepository,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var userId = currentUser.UserId.Value;
                var account = await accountRepository.GetByUserIdAsync(userId);
                var todayCheckedIn = await transactionRepository.ExistsBizIdAsync(
                    $"checkin:{userId:N}:{DateTime.UtcNow:yyyyMMdd}");

                return ApiResponseHelper.Ok(new
                {
                    balance = account?.Balance ?? 0,
                    totalEarned = account?.TotalEarned ?? 0,
                    totalSpent = account?.TotalSpent ?? 0,
                    todayCheckedIn,
                    consecutiveDays = account?.ConsecutiveCheckinDays ?? 0
                }, httpContext: httpContext);
            })
            .WithName("GetPointAccount")
            .WithSummary("积分账户概览")
            .WithDescription("余额、累计、今日签到状态与连续签到天数");

            /// <summary>
            /// 每日签到：阶梯分值实时计算，同日重复返回已签状态（幂等）
            /// </summary>
            group.MapPost("/checkin", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IPointsService pointsService,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var result = await pointsService.CheckinAsync(currentUser.UserId.Value);
                return ApiResponseHelper.Ok(new
                {
                    amount = result.Amount,
                    consecutiveDays = result.ConsecutiveDays,
                    alreadyCheckedIn = result.AlreadyCheckedIn
                }, httpContext: httpContext);
            })
            .WithName("DailyCheckin")
            .WithSummary("每日签到")
            .WithDescription("主动签到得阶梯积分，连续天数越多分值越高（封顶末档）");

            /// <summary>
            /// 积分流水分页（明细页数据源，时间倒序）
            /// </summary>
            group.MapGet("/transactions", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IPointTransactionRepository transactionRepository,
                HttpContext httpContext,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var (items, total) = await transactionRepository.GetByUserAsync(
                    currentUser.UserId.Value, page, Math.Min(pageSize, 100));
                return ApiResponseHelper.Paged(items.Select(t => new
                {
                    id = t.Id,
                    direction = t.Direction,
                    grantType = t.GrantType,
                    amount = t.Amount,
                    balanceAfter = t.BalanceAfter,
                    remark = t.Remark,
                    createdAt = t.CreatedAt
                }).ToList(), total, page, pageSize, httpContext: httpContext);
            })
            .WithName("GetPointTransactions")
            .WithSummary("积分流水")
            .WithDescription("个人积分收支明细分页");

            /// <summary>
            /// 赚分任务清单（v1.33.0 任务中心数据源）：启用中的规则 + 本人完成态。
            /// 完成态口径：daily 类看今日流水、once 类看历史流水；映射在代码（新 grantType
            /// 本就要写消费场景代码，规则表只管分值不管语义）
            /// </summary>
            group.MapGet("/tasks", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IPointGrantRuleRepository ruleRepository,
                [FromServices] IPointTransactionRepository transactionRepository,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var userId = currentUser.UserId.Value;
                var rules = await ruleRepository.GetAllAsync();
                var todayStart = DateTime.UtcNow.Date;
                var doneToday = await transactionRepository.GetGrantTypesAsync(userId, todayStart);
                var doneEver = await transactionRepository.GetGrantTypesAsync(userId, null);

                // 任务节奏类型：daily=每日可完成 / once=一次性
                var dailyTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    PointTransaction.TypeDailyLogin, PointTransaction.TypeDailyCheckin, PointTransaction.TypeCorrectionAdopted
                };

                var tasks = rules.Where(r => r.IsEnabled).Select(r =>
                {
                    var isDaily = dailyTypes.Contains(r.GrantType);
                    return new
                    {
                        grantType = r.GrantType,
                        displayName = r.DisplayName,
                        amount = r.Amount,
                        description = r.Description,
                        // 阶梯动作返回起步分值+阶梯数组，前端可展示"最高 X 分"
                        ladder = PointTaskHelper.ParseLadder(r.LadderJson),
                        daily = isDaily,
                        done = isDaily ? doneToday.Contains(r.GrantType) : doneEver.Contains(r.GrantType)
                    };
                }).ToList();
                return ApiResponseHelper.Ok(tasks, httpContext: httpContext);
            })
            .WithName("GetPointTasks")
            .WithSummary("赚分任务清单")
            .WithDescription("任务中心数据源：启用规则+完成态（daily 看今日、once 看历史）");

            // ============ Admin 端（规则配置） ============
            var adminGroup = app.MapGroup("/api/admin/points").RequireAuthorization();

            /// <summary>
            /// 积分规则列表（Admin 积分任务管理页数据源）
            /// </summary>
            adminGroup.MapGet("/rules", async (
                [FromServices] IPointGrantRuleRepository ruleRepository,
                HttpContext httpContext) =>
            {
                var rules = await ruleRepository.GetAllAsync();
                return ApiResponseHelper.Ok(rules.Select(r => new
                {
                    id = r.Id,
                    grantType = r.GrantType,
                    displayName = r.DisplayName,
                    amount = r.Amount,
                    dailyLimit = r.DailyLimit,
                    ladderJson = r.LadderJson,
                    isEnabled = r.IsEnabled,
                    description = r.Description
                }).ToList(), httpContext: httpContext);
            })
            .WithName("GetPointRules")
            .WithSummary("积分规则列表")
            .WithDescription("全部赚分规则（分值/每日上限/阶梯/开关）")
            .RequirePermission("system.manage");

            /// <summary>
            /// 更新积分规则（分值/上限/阶梯/开关，改完实时生效）
            /// </summary>
            adminGroup.MapPut("/rules/{id:guid}", async (
                Guid id,
                [FromBody] UpdatePointRuleRequest request,
                [FromServices] IPointGrantRuleRepository ruleRepository,
                [FromServices] OpenFindBearings.Application.Shared.Interfaces.IUnitOfWork unitOfWork,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var rules = await ruleRepository.GetAllAsync(cancellationToken);
                var rule = rules.FirstOrDefault(r => r.Id == id);
                if (rule == null)
                    return ApiResponseHelper.NotFound("规则不存在", httpContext);

                if (request.Amount.HasValue) rule.ChangeAmount(request.Amount.Value);
                if (request.DailyLimit.HasValue) rule.ChangeDailyLimit(request.DailyLimit.Value);
                // 改动说明：空串=清除阶梯（Admin 表单留空提交 ""，与 null"不修改"区分）
                if (request.LadderJson != null) rule.ChangeLadder(string.IsNullOrWhiteSpace(request.LadderJson) ? null : request.LadderJson);
                if (request.IsEnabled.HasValue) rule.SetEnabled(request.IsEnabled.Value);
                await ruleRepository.UpdateAsync(rule, cancellationToken);
                // 端点直调仓储不经 MediatR 管道，需显式提交（与 PointsService 同款）
                await unitOfWork.SaveChangesAsync(cancellationToken);

                return ApiResponseHelper.Ok("规则已更新", httpContext);
            })
            .WithName("UpdatePointRule")
            .WithSummary("更新积分规则")
            .WithDescription("调整分值/每日上限/阶梯/启停，实时生效")
            .RequirePermission("system.manage");
        }
    }

    /// <summary>
    /// 积分规则更新请求（字段可空=不修改）
    /// </summary>
    /// <param name="Amount">基础分值</param>
    /// <param name="DailyLimit">每日上限（0=不限）</param>
    /// <param name="LadderJson">连续阶梯 JSON 数组</param>
    /// <param name="IsEnabled">启用开关</param>
    public record UpdatePointRuleRequest(int? Amount, int? DailyLimit, string? LadderJson, bool? IsEnabled);

    internal static class PointTaskHelper
    {
        /// <summary>
        /// 解析阶梯 JSON 数组（非法/空返回 null，任务中心据此展示"最高 X 分"）
        /// </summary>
        /// <param name="ladderJson">阶梯 JSON 字符串</param>
        public static List<int>? ParseLadder(string? ladderJson)
        {
            if (string.IsNullOrWhiteSpace(ladderJson))
                return null;
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<int>>(ladderJson);
            }
            catch (System.Text.Json.JsonException)
            {
                return null;
            }
        }
    }
}
