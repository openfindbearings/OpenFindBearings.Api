using MediatR;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Api.Services;
using OpenFindBearings.Application.Features.ApiLogs.Commands;
using OpenFindBearings.Application.Features.Users.Commands;
using OpenFindBearings.Application.Interfaces;
using OpenFindBearings.Domain.Entities;
using System.Diagnostics;

namespace OpenFindBearings.Api.Middleware
{
    /// <summary>
    /// API 日志中间件
    /// 合并了请求日志和用户行为收集功能
    /// </summary>
    public class ApiLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiLoggingMiddleware> _logger;
        private readonly IBackgroundTaskQueue _taskQueue;

        public ApiLoggingMiddleware(
            RequestDelegate next,
            ILogger<ApiLoggingMiddleware> logger,
            IBackgroundTaskQueue taskQueue)
        {
            _next = next;
            _logger = logger;
            _taskQueue = taskQueue;
        }

        public async Task InvokeAsync(HttpContext context, IMediator mediator, IIpRegionService regionService)
        {
            var stopwatch = Stopwatch.StartNew();
            var userId = context.GetUserId();
            var sessionId = context.GetSessionId();
            var clientIp = context.GetClientIp();
            var userAgent = context.GetUserAgent();
            var apiPath = context.Request.Path;
            var httpMethod = context.Request.Method;

            // 记录请求开始
            _logger.LogInformation(
                "HTTP请求 {Method} {Path} - IP: {IP}, UserId: {UserId}",
                httpMethod, apiPath, clientIp, userId);

            // ✅ 不替换 Response.Body，直接调用
            await _next(context);

            stopwatch.Stop();

            var statusCode = context.Response.StatusCode;
            var durationMs = (int)stopwatch.ElapsedMilliseconds;

            // 记录响应
            _logger.LogInformation(
                "HTTP响应 {Method} {Path} - 状态码: {StatusCode}, 耗时: {Duration}ms",
                httpMethod, apiPath, statusCode, durationMs);

            // 异步记录到数据库
            _taskQueue.QueueBackgroundWorkItem(async (serviceProvider, token) =>
            {
                try
                {
                    var scopedMediator = serviceProvider.GetRequiredService<IMediator>();

                    // 1. 记录 API 调用日志
                    var apiLog = new ApiCallLog(
                        userId: userId,
                        sessionId: sessionId,
                        apiPath: apiPath,
                        httpMethod: httpMethod,
                        statusCode: statusCode,
                        durationMs: durationMs,
                        clientIp: clientIp,
                        userAgent: userAgent);

                    await scopedMediator.Send(new AddApiCallLogCommand { Log = apiLog }, token);

                    // 2. 记录用户偏好（仅登录用户的搜索/查看行为）
                    if (userId.HasValue && IsSearchOrViewAction(apiPath))
                    {
                        var scopedRegionService = serviceProvider.GetRequiredService<IIpRegionService>();
                        var region = await scopedRegionService.GetRegionByIpAsync(clientIp ?? "unknown");

                        if (region.HasValue && (region.Value.Province != null || region.Value.City != null))
                        {
                            await scopedMediator.Send(new UpdateUserPreferenceCommand
                            {
                                UserId = userId.Value,
                                Province = region.Value.Province,
                                City = region.Value.City
                            }, token);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "后台任务执行失败");
                }
            });
        }

        /// <summary>
        /// 判断是否是搜索或查看行为（需要记录地区偏好）
        /// </summary>
        private bool IsSearchOrViewAction(string path)
        {
            return path.Contains("/bearings/search") ||
                   path.Contains("/bearings/by-code") ||
                   (path.Contains("/bearings/") &&
                    !path.Contains("/search") &&
                    !path.Contains("/hot")) ||
                   (path.Contains("/merchants/") &&
                    path.Contains("/bearings"));
        }
    }
}
