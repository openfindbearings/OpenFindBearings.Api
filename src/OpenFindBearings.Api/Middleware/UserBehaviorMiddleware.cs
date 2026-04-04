using MediatR;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Api.Services;
using OpenFindBearings.Application.Features.ApiLogs.Commands;
using OpenFindBearings.Application.Features.Users.Commands;
using OpenFindBearings.Domain.Entities;
using System.Diagnostics;

namespace OpenFindBearings.Api.Middleware
{
    /// <summary>
    /// 用户行为收集中间件
    /// 记录 API 调用日志，用于防滥用和用户行为分析
    /// </summary>
    public class UserBehaviorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UserBehaviorMiddleware> _logger;

        public UserBehaviorMiddleware(RequestDelegate next, ILogger<UserBehaviorMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// 调用中间件
        /// </summary>
        public async Task InvokeAsync(
            HttpContext context,
            IMediator mediator,
            IIpRegionService regionService)
        {
            var stopwatch = Stopwatch.StartNew();

            var userId = context.GetUserId();
            var sessionId = context.GetSessionId();
            var clientIp = context.GetClientIp();
            var userAgent = context.GetUserAgent();

            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                // 创建 API 调用日志
                var apiLog = new ApiCallLog(
                    userId: userId,
                    sessionId: sessionId,
                    apiPath: context.Request.Path,
                    httpMethod: context.Request.Method,
                    statusCode: context.Response.StatusCode,
                    durationMs: (int)stopwatch.ElapsedMilliseconds,
                    clientIp: clientIp,
                    userAgent: userAgent);

                // 异步记录 API 调用日志，不阻塞响应
                _ = Task.Run(() => mediator.Send(new AddApiCallLogCommand { Log = apiLog }));

                // 记录用户地区偏好（仅登录用户，且是搜索或查看行为）
                if (userId.HasValue && IsSearchOrViewAction(context.Request.Path))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // 解析 IP 对应的地区
                            var region = await regionService.GetRegionByIpAsync(clientIp ?? "unknown");
                            if (region.HasValue && (region.Value.Province != null || region.Value.City != null))
                            {
                                // 更新用户地区偏好
                                await mediator.Send(new UpdateUserRegionPreferenceCommand
                                {
                                    UserId = userId.Value,
                                    Province = region.Value.Province,
                                    City = region.Value.City
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "记录地区偏好失败: UserId={UserId}", userId);
                        }
                    });
                }

                // 恢复响应流
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        /// <summary>
        /// 判断是否是搜索或查看行为
        /// 这些行为需要记录地区偏好
        /// </summary>
        private bool IsSearchOrViewAction(string path)
        {
            return path.Contains("/bearings/search") ||           // 搜索轴承
                   path.Contains("/bearings/by-code") ||          // 通过型号查询
                   (path.Contains("/bearings/") &&
                    !path.Contains("/search") &&
                    !path.Contains("/hot")) ||                    // 查看轴承详情
                   path.Contains("/merchants/") &&
                    path.Contains("/bearings");                   // 查看商家轴承列表
        }
    }
}
