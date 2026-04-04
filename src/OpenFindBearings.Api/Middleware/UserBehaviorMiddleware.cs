using MediatR;
using OpenFindBearings.Api.Helpers;
using OpenFindBearings.Application.Features.ApiLogs.Commands;
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

        public async Task InvokeAsync(HttpContext context, IMediator mediator)
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

                var apiLog = new ApiCallLog(
                    userId: userId,
                    sessionId: sessionId,
                    apiPath: context.Request.Path,
                    httpMethod: context.Request.Method,
                    statusCode: context.Response.StatusCode,
                    durationMs: (int)stopwatch.ElapsedMilliseconds,
                    clientIp: clientIp,
                    userAgent: userAgent);

                // 异步记录，不阻塞响应
                _ = Task.Run(() => mediator.Send(new AddApiCallLogCommand { Log = apiLog }));

                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
    }
}
