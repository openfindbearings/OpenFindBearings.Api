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
    /// 用户行为收集中间件
    /// 记录 API 调用日志，用于防滥用和用户行为分析
    /// </summary>
    public class UserBehaviorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UserBehaviorMiddleware> _logger;
        private readonly IBackgroundTaskQueue _taskQueue;

        public UserBehaviorMiddleware(
            RequestDelegate next,
            ILogger<UserBehaviorMiddleware> logger,
            IBackgroundTaskQueue taskQueue)
        {
            _next = next;
            _logger = logger;
            _taskQueue = taskQueue;
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

                // 保存当前请求的响应信息（在 finally 块中捕获）
                var apiPath = context.Request.Path;
                var httpMethod = context.Request.Method;
                var statusCode = context.Response.StatusCode;
                var durationMs = (int)stopwatch.ElapsedMilliseconds;
                var currentUserId = userId;
                var currentSessionId = sessionId;
                var currentClientIp = clientIp;
                var currentUserAgent = userAgent;
                var isSearchOrView = IsSearchOrViewAction(apiPath);
                var currentUserIdValue = userId;

                // 使用后台任务队列记录 API 调用日志
                _taskQueue.QueueBackgroundWorkItem(async (serviceProvider, token) =>
                {
                    try
                    {
                        var scopedMediator = serviceProvider.GetRequiredService<IMediator>();

                        var apiLog = new ApiCallLog(
                            userId: currentUserId,
                            sessionId: currentSessionId,
                            apiPath: apiPath,
                            httpMethod: httpMethod,
                            statusCode: statusCode,
                            durationMs: durationMs,
                            clientIp: currentClientIp,
                            userAgent: currentUserAgent);

                        await scopedMediator.Send(new AddApiCallLogCommand { Log = apiLog }, token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "记录 API 调用日志失败");
                    }
                });

                // 使用后台任务队列记录地区偏好
                if (currentUserId.HasValue && isSearchOrView)
                {
                    _taskQueue.QueueBackgroundWorkItem(async (serviceProvider, token) =>
                    {
                        try
                        {
                            var scopedMediator = serviceProvider.GetRequiredService<IMediator>();
                            var scopedRegionService = serviceProvider.GetRequiredService<IIpRegionService>();

                            var region = await scopedRegionService.GetRegionByIpAsync(currentClientIp ?? "unknown");
                            if (region.HasValue && (region.Value.Province != null || region.Value.City != null))
                            {
                                await scopedMediator.Send(new UpdateUserRegionPreferenceCommand
                                {
                                    UserId = currentUserIdValue!.Value,
                                    Province = region.Value.Province,
                                    City = region.Value.City
                                }, token);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "记录地区偏好失败: UserId={UserId}", currentUserId);
                        }
                    });
                }

                // 恢复响应流
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        /// <summary>
        /// 判断是否是搜索或查看行为
        /// </summary>
        private bool IsSearchOrViewAction(string path)
        {
            return path.Contains("/bearings/search") ||
                   path.Contains("/bearings/by-code") ||
                   (path.Contains("/bearings/") &&
                    !path.Contains("/search") &&
                    !path.Contains("/hot")) ||
                   path.Contains("/merchants/") &&
                    path.Contains("/bearings");
        }
    }
}
