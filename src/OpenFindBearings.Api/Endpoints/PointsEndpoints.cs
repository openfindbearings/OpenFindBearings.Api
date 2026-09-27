using Microsoft.AspNetCore.Mvc;
using OpenFindBearings.Api.Extensions;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Api.Services;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Services;
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
                    $"checkin:{userId:N}:{BusinessClock.DateKey}");

                return ApiResponseHelper.Ok(new
                {
                    balance = account?.Balance ?? 0,
                    totalEarned = account?.TotalEarned ?? 0,
                    totalSpent = account?.TotalSpent ?? 0,
                    todayCheckedIn,
                    consecutiveDays = account?.ConsecutiveCheckinDays ?? 0,
                    // v1.36.1：下发业务日界偏移——前端日期条/对勾按此换算，防管理员改配置后前端硬编码 +8 漂移
                    tzOffsetHours = (int)BusinessClock.Offset.TotalHours
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
                [FromServices] IAchievementService achievementService,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var result = await pointsService.CheckinAsync(currentUser.UserId.Value);

                // 改动说明（v2.1.0 成就子系统）：签到成功后驱动忠诚类成就（连签仪表+签到总数计数）；
                // 成就失败绝不影响签到主流程；新点亮键回传供前端 toast
                var unlocked = new List<string>();
                if (!result.AlreadyCheckedIn)
                {
                    try
                    {
                        unlocked.AddRange(await achievementService.SetGaugeAsync(
                            Domain.Entities.AchievementScope.Personal, currentUser.UserId.Value,
                            "checkin_streak", result.ConsecutiveDays));
                        unlocked.AddRange(await achievementService.IncrementAsync(
                            Domain.Entities.AchievementScope.Personal, currentUser.UserId.Value,
                            "checkin_total", 1));
                    }
                    catch { /* 成就为旁路增强，吞掉不反噬签到 */ }
                }

                return ApiResponseHelper.Ok(new
                {
                    amount = result.Amount,
                    consecutiveDays = result.ConsecutiveDays,
                    alreadyCheckedIn = result.AlreadyCheckedIn,
                    unlockedAchievements = unlocked
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
                var todayStart = BusinessClock.TodayUtc;
                var doneToday = await transactionRepository.GetGrantTypesAsync(userId, todayStart);
                var doneEver = await transactionRepository.GetGrantTypesAsync(userId, null);
                // 改动说明（v1.34.0）：daily 任务今日完成次数（任务中心显示"今日 n/上限"）
                var countsToday = await transactionRepository.GetGrantCountsAsync(userId, todayStart);

                // 任务节奏类型：daily=每日可完成 / once=一次性
                var dailyTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    PointTransaction.TypeDailyLogin, PointTransaction.TypeDailyCheckin, PointTransaction.TypeCorrectionAdopted
                };

                // 改动说明（v1.36.1）：寻货加量两条规则是**消费定价**不是赚分任务，
                // 从任务清单过滤（Admin 规则页保留可调价）；否则任务中心出现"自动发放"的伪任务
                // 改动说明（v2.6.0 M3）：merchant_task 是集体任务 Job 结算的被动发放（个人无对应动作），
                // 同口径过滤——它的展示面是商家集体任务卡
                var pricingOnlyTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "sourcing_publish_bonus", "sourcing_respond_bonus", PointTransaction.TypeMerchantTask
                };

                var tasks = rules.Where(r => r.IsEnabled && !pricingOnlyTypes.Contains(r.GrantType)).Select(r =>
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
                        done = isDaily ? doneToday.Contains(r.GrantType) : doneEver.Contains(r.GrantType),
                        // 今日完成次数与每日上限（daily 有意义；limit=0 表示不限）
                        count = countsToday.TryGetValue(r.GrantType, out var cnt) ? cnt : 0,
                        limit = r.DailyLimit
                    };
                }).ToList();
                return ApiResponseHelper.Ok(tasks, httpContext: httpContext);
            })
            .WithName("GetPointTasks")
            .WithSummary("赚分任务清单")
            .WithDescription("任务中心数据源：启用规则+完成态（daily 看今日、once 看历史）");

            /// <summary>
            /// 商家福利卡（v2.5.0 商家经济）：成员最佳商家等级 + buff 清单 + 升下一级条件。
            /// 散人返回 grade=0 空清单（前端展示"加入商家可享商家加成"引导）
            /// </summary>
            group.MapGet("/merchant-buff", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMerchantGradeService grades,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var best = await grades.GetBestForUserAsync(currentUser.UserId.Value, httpContext.RequestAborted);
                var rank = best == null ? 0 : OpenFindBearings.Application.Services.MerchantBuffs.Rank(best.Grade);
                string nextHint = rank switch
                {
                    0 => "加入认证商家可享：签到 +1、寻货应答 +1/日",
                    1 => "商户通过认证后解锁：登录 +1、纠错 +10%、发布 +1/日",
                    2 => "在售满 5 件升活跃供给：纠错 +20%、应答 +3/日、置顶 9 折",
                    3 => "在售满 10 件且金库累计 500 升金牌：纠错 +25%、应答 +5/日、置顶 8 折",
                    _ => "已达最高等级"
                };

                return ApiResponseHelper.Ok(new
                {
                    merchantId = best?.MerchantId,
                    merchantName = best?.MerchantName,
                    grade = best?.Grade ?? 0,
                    rank,
                    labels = OpenFindBearings.Application.Services.MerchantBuffs.BuffLabels(best?.Grade ?? 0),
                    nextHint
                }, httpContext: httpContext);
            })
            .WithName("GetMerchantBuff")
            .WithSummary("商家福利卡")
            .WithDescription("成员被动加成数据源：等级/福利清单/升级提示（散人为空）");

            /// <summary>
            /// 商家集体任务板（v2.6.0 M3）：成员视角——最佳商家的启用任务 + 本周期进度 + 完成态。
            /// 散人返回空清单（与福利卡同口径：任务跟随最佳商家，不要求切换当前商户上下文）
            /// </summary>
            group.MapGet("/merchant-tasks", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMerchantGradeService grades,
                [FromServices] IMerchantTaskService taskService,
                [FromServices] IMerchantTaskRepository taskRepository,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var best = await grades.GetBestForUserAsync(currentUser.UserId.Value, httpContext.RequestAborted);
                if (best == null)
                    return ApiResponseHelper.Ok(new { merchantId = (Guid?)null, merchantName = (string?)null, tasks = Array.Empty<object>(), completedTotal = 0 }, httpContext: httpContext);

                var tasks = await taskService.GetTasksForMerchantAsync(best.MerchantId, httpContext.RequestAborted);
                // 累计完成次数（raid 团本通关数，展示商家集体成就感）
                var completedTotal = await taskRepository.CountCompletionsAsync(best.MerchantId, httpContext.RequestAborted);
                return ApiResponseHelper.Ok(new
                {
                    merchantId = best.MerchantId,
                    merchantName = best.MerchantName,
                    tasks = tasks.Select(t => new
                    {
                        taskKey = t.TaskKey,
                        name = t.Name,
                        description = t.Description,
                        target = t.Target,
                        current = t.Current,
                        period = t.Period,
                        rewardType = t.RewardType,
                        rewardAmount = t.RewardAmount,
                        done = t.Done
                    }),
                    completedTotal
                }, httpContext: httpContext);
            })
            .WithName("GetMerchantTasks")
            .WithSummary("商家集体任务板")
            .WithDescription("最佳商家的周期任务进度与完成态（散人为空清单）");

            /// <summary>
            /// 商家实力月榜（v2.6.0 M3）：本月金库入账 TOP 榜 + 本人最佳商家单独回显。
            /// 只展示商家名/等级/入账额（B2B 供给侧数据，无个人信息隐私负担）
            /// </summary>
            group.MapGet("/merchant-ranking", async (
                [FromServices] ICurrentUserService currentUser,
                [FromServices] IMerchantGradeService grades,
                [FromServices] IMerchantTaskService taskService,
                HttpContext httpContext) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var best = await grades.GetBestForUserAsync(currentUser.UserId.Value, httpContext.RequestAborted);
                var result = await taskService.GetMonthlyRankingAsync(best?.MerchantId, httpContext.RequestAborted);
                return ApiResponseHelper.Ok(new
                {
                    periodKey = result.PeriodKey,
                    top = result.Top.Select(r => new
                    {
                        rank = r.Rank,
                        merchantId = r.MerchantId,
                        merchantName = r.MerchantName,
                        gradeDisplay = r.GradeDisplay,
                        total = r.Total
                    }),
                    mine = result.Mine == null ? null : new
                    {
                        rank = result.Mine.Rank,
                        merchantId = result.Mine.MerchantId,
                        merchantName = result.Mine.MerchantName,
                        gradeDisplay = result.Mine.GradeDisplay,
                        total = result.Mine.Total
                    }
                }, httpContext: httpContext);
            })
            .WithName("GetMerchantRanking")
            .WithSummary("商家实力月榜")
            .WithDescription("本月金库入账 TOP 榜 + 我的商家回显（rank=0 为未上榜）");

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
            .RequirePermission("points.manage");

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
            .RequirePermission("points.manage");

            // ============ Admin 端（v2.6.0 商家集体任务定义） ============

            /// <summary>
            /// 集体任务定义列表（Admin 管理页数据源，含停用项）
            /// </summary>
            adminGroup.MapGet("/merchant-tasks", async (
                [FromServices] IMerchantTaskRepository taskRepository,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var defs = await taskRepository.GetAllAsync(cancellationToken);
                return ApiResponseHelper.Ok(defs.Select(d => new
                {
                    id = d.Id,
                    taskKey = d.TaskKey,
                    name = d.Name,
                    description = d.Description,
                    metricKey = d.MetricKey,
                    targetValue = d.TargetValue,
                    period = d.Period,
                    rewardType = d.RewardType,
                    rewardAmount = d.RewardAmount,
                    enabled = d.Enabled,
                    sortOrder = d.SortOrder
                }).ToList(), httpContext: httpContext);
            })
            .WithName("GetMerchantTaskDefinitions")
            .WithSummary("商家集体任务列表")
            .WithDescription("全部任务定义（指标/目标/周期/奖励/开关）")
            .RequirePermission("points.manage");

            /// <summary>
            /// 新建集体任务定义（TaskKey 全局唯一——台账与奖励 bizId 锚定它，创建后不可改）
            /// </summary>
            adminGroup.MapPost("/merchant-tasks", async (
                [FromBody] CreateMerchantTaskRequest request,
                [FromServices] IMerchantTaskRepository taskRepository,
                [FromServices] OpenFindBearings.Application.Shared.Interfaces.IUnitOfWork unitOfWork,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var error = ValidateMerchantTaskRequest(request.TaskKey, request.Name, request.Description,
                    request.MetricKey, request.TargetValue, request.Period, request.RewardType, request.RewardAmount);
                if (error != null)
                    return ApiResponseHelper.BadRequest(error, httpContext: httpContext);
                var defs = await taskRepository.GetAllAsync(cancellationToken);
                if (defs.Any(d => string.Equals(d.TaskKey, request.TaskKey.Trim(), StringComparison.Ordinal)))
                    return ApiResponseHelper.BadRequest("任务键已存在", httpContext: httpContext);

                var definition = MerchantTaskDefinition.Create(request.TaskKey.Trim(), request.Name.Trim(),
                    request.Description?.Trim() ?? "", request.MetricKey, request.TargetValue,
                    request.Period, request.RewardType, request.RewardAmount, request.SortOrder);
                await taskRepository.AddDefinitionAsync(definition, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return ApiResponseHelper.Ok(new { id = definition.Id }, "任务已创建", httpContext: httpContext);
            })
            .WithName("CreateMerchantTaskDefinition")
            .WithSummary("新建商家集体任务")
            .WithDescription("创建周期任务定义（TaskKey 不可变）")
            .RequirePermission("points.manage");

            /// <summary>
            /// 编辑集体任务定义（TaskKey 不可变，数值与开关改完实时生效）
            /// </summary>
            adminGroup.MapPut("/merchant-tasks/{id:guid}", async (
                Guid id,
                [FromBody] UpdateMerchantTaskRequest request,
                [FromServices] IMerchantTaskRepository taskRepository,
                [FromServices] OpenFindBearings.Application.Shared.Interfaces.IUnitOfWork unitOfWork,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var definition = await taskRepository.GetByIdAsync(id, cancellationToken);
                if (definition == null)
                    return ApiResponseHelper.NotFound("任务不存在", httpContext);
                var error = ValidateMerchantTaskRequest(definition.TaskKey, request.Name, request.Description,
                    request.MetricKey, request.TargetValue, request.Period, request.RewardType, request.RewardAmount);
                if (error != null)
                    return ApiResponseHelper.BadRequest(error, httpContext: httpContext);

                definition.Update(request.Name.Trim(), request.Description?.Trim() ?? "", request.MetricKey,
                    request.TargetValue, request.Period, request.RewardType, request.RewardAmount,
                    request.Enabled, request.SortOrder);
                await taskRepository.UpdateDefinitionAsync(definition, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return ApiResponseHelper.Ok("任务已更新", httpContext);
            })
            .WithName("UpdateMerchantTaskDefinition")
            .WithSummary("更新商家集体任务")
            .WithDescription("调整目标/周期/奖励/启停，实时生效")
            .RequirePermission("points.manage");
        }

        /// <summary>
        /// 任务定义请求公共校验（键/名必填、指标与周期与奖励枚举合法、数值为正）；通过返回 null
        /// </summary>
        private static string? ValidateMerchantTaskRequest(string? taskKey, string? name, string? description,
            string? metricKey, int targetValue, int period, int rewardType, int rewardAmount)
        {
            if (string.IsNullOrWhiteSpace(taskKey) || taskKey.Trim().Length > 64)
                return "任务键必填且不超过 64 字符";
            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 64)
                return "任务名必填且不超过 64 字符";
            if (description != null && description.Length > 256)
                return "描述不超过 256 字符";
            if (metricKey is not (MerchantTaskDefinition.MetricCorrections or MerchantTaskDefinition.MetricTreasury
                or MerchantTaskDefinition.MetricProducts))
                return "指标仅支持 corrections/treasury/products";
            if (period is not (MerchantTaskDefinition.PeriodWeekly or MerchantTaskDefinition.PeriodMonthly))
                return "周期仅支持 1 周 / 2 月";
            if (rewardType is not (MerchantTaskDefinition.RewardMembers or MerchantTaskDefinition.RewardTreasury))
                return "奖励对象仅支持 1 成员 / 2 金库";
            if (targetValue <= 0 || rewardAmount <= 0)
                return "目标值与奖励分值须为正";
            return null;
        }
    }

    /// <summary>集体任务新建请求（TaskKey 创建后不可变）</summary>
    public record CreateMerchantTaskRequest(string TaskKey, string Name, string? Description,
        string MetricKey, int TargetValue, int Period, int RewardType, int RewardAmount, int SortOrder);

    /// <summary>集体任务编辑请求（TaskKey 不在字段内——锚定台账不可改）</summary>
    public record UpdateMerchantTaskRequest(string Name, string? Description,
        string MetricKey, int TargetValue, int Period, int RewardType, int RewardAmount, bool Enabled, int SortOrder);

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
