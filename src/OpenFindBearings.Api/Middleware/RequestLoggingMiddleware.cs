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

            // 记录请求信息（可选，不读取 body 以避免性能问题）
            LogRequest(context);

            // 捕获响应 - 使用原始流直接写入，不读取响应体
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);

                stopwatch.Stop();

                // 记录响应基本信息（不读取 body）
                _logger.LogInformation(
                    "HTTP响应 {Method} {Path} - 状态码: {StatusCode}, 耗时: {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);

                // 将响应内容写回原始流
                await responseBody.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "请求处理异常 {Method} {Path} - 耗时: {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        /// <summary>
        /// 记录请求信息（同步，不读取 body）
        /// </summary>
        private void LogRequest(HttpContext context)
        {
            // 只记录基本信息，不读取请求体（避免性能问题和流关闭问题）
            _logger.LogInformation(
                "HTTP请求 {Method} {Path} - IP: {IP}, UserAgent: {UserAgent}",
                context.Request.Method,
                context.Request.Path,
                context.Connection.RemoteIpAddress,
                context.Request.Headers["User-Agent"].FirstOrDefault());
        }
    }
}
