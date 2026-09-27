using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OpenFindBearings.Api.Extensions;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Api.Services;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Services;

namespace OpenFindBearings.Api.Endpoints
{
    /// <summary>
    /// 游戏中心统一端点（v2.10.1 方案 A 插件架构）：
    /// /api/games/{key}/board 出题、/api/games/{key}/result 结算发分，按 IGameProvider.Key 路由。
    /// 端点组刻意独立于积分/商城——将来游戏拆微服务时整组平移，主 API 只留 PointsService 发分口。
    /// </summary>
    public static class GameEndpoints
    {
        /// <summary>
        /// 映射游戏端点（provider 集合由 DI 注入，加游戏=注册新 IGameProvider 实现）
        /// </summary>
        public static void MapGameEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/games").RequireAuthorization();

            /// <summary>
            /// 出一局题板（登录用户；未知游戏键 404）
            /// </summary>
            group.MapGet("/{key}/board", async (
                string key,
                [FromServices] IEnumerable<IGameProvider> providers,
                [FromServices] ICurrentUserService currentUser,
                HttpContext httpContext,
                [FromQuery] int? size = null) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var provider = providers.FirstOrDefault(p => p.Key == key);
                if (provider == null)
                    return ApiResponseHelper.NotFound("游戏不存在", httpContext);

                var board = await provider.GetBoardAsync(currentUser.UserId.Value, size, httpContext.RequestAborted);
                return ApiResponseHelper.Ok(board, httpContext: httpContext);
            })
            .WithName("GetGameBoard")
            .WithSummary("游戏出题板")
            .WithDescription("按游戏键出题（连连看=随机有图轴承对，类型打散），size=规模参数");

            /// <summary>
            /// 结算一局结果（幂等发分；granted=0 表示今日额度满或重复提交）
            /// </summary>
            group.MapPost("/{key}/result", async (
                string key,
                [FromServices] IEnumerable<IGameProvider> providers,
                [FromServices] ICurrentUserService currentUser,
                HttpContext httpContext,
                JsonElement payload) =>
            {
                if (!currentUser.UserId.HasValue)
                    return ApiResponseHelper.Unauthorized(httpContext: httpContext);

                var provider = providers.FirstOrDefault(p => p.Key == key);
                if (provider == null)
                    return ApiResponseHelper.NotFound("游戏不存在", httpContext);

                var granted = await provider.ReportResultAsync(
                    currentUser.UserId.Value, payload, httpContext.RequestAborted);
                return ApiResponseHelper.Ok(new { granted }, httpContext: httpContext);
            })
            .WithName("ReportGameResult")
            .WithSummary("游戏结算发分")
            .WithDescription("各 Provider 自定义 payload（连连看={gameId}），幂等 bizId + 规则表日限双保险");
        }
    }
}
