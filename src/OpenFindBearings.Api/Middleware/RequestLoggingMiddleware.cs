using System.Diagnostics;
using System.Text;

namespace OpenFindBearings.Api.Middleware
{
    /// <summary>
    /// 请求日志中间件
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            // 记录请求信息
            await LogRequest(context);

            // 捕获响应
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);

                // 记录响应信息
                await LogResponse(context, stopwatch);

                // 将响应内容写回
                await responseBody.CopyToAsync(originalBodyStream);
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private async Task LogRequest(HttpContext context)
        {
            context.Request.EnableBuffering();

            var requestBody = await ReadRequestBody(context.Request);

            _logger.LogInformation(
                "HTTP请求 {Method} {Path} - IP: {IP}, UserAgent: {UserAgent}, Body: {Body}",
                context.Request.Method,
                context.Request.Path,
                context.Connection.RemoteIpAddress,
                context.Request.Headers["User-Agent"].FirstOrDefault(),
                requestBody);

            context.Request.Body.Position = 0;
        }

        private async Task LogResponse(HttpContext context, Stopwatch stopwatch)
        {
            stopwatch.Stop();

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            _logger.LogInformation(
                "HTTP响应 {Method} {Path} - 状态码: {StatusCode}, 耗时: {ElapsedMs}ms, Body: {Body}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                responseBody);
        }

        private static async Task<string> ReadRequestBody(HttpRequest request)
        {
            if (request.Body == null || !request.Body.CanRead)
                return string.Empty;

            if (request.ContentLength > 0 && request.ContentLength < 2048) // 只记录小于2KB的body
            {
                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                return await reader.ReadToEndAsync();
            }

            return "[内容太大或空]";
        }
    }
}
