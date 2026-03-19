using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace OpenFindBearings.Api.Middleware
{
    /// <summary>
    /// 全局异常处理中间件
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "发生未处理异常");

            var response = context.Response;
            response.ContentType = "application/json";

            var problemDetails = new ProblemDetails
            {
                Instance = context.Request.Path,
                Extensions = { ["traceId"] = context.TraceIdentifier }
            };

            switch (exception)
            {
                case ValidationException validationException:
                    response.StatusCode = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "请求参数验证失败";
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Detail = "请检查输入参数";
                    problemDetails.Extensions["errors"] = validationException.Errors
                        .Select(e => new { e.PropertyName, e.ErrorMessage });
                    break;

                case InvalidOperationException invalidOperation:
                    response.StatusCode = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "操作无效";
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Detail = invalidOperation.Message;
                    break;

                case UnauthorizedAccessException:
                    response.StatusCode = StatusCodes.Status401Unauthorized;
                    problemDetails.Title = "未授权";
                    problemDetails.Status = StatusCodes.Status401Unauthorized;
                    problemDetails.Detail = "请先登录";
                    break;

                case KeyNotFoundException notFound:
                    response.StatusCode = StatusCodes.Status404NotFound;
                    problemDetails.Title = "资源不存在";
                    problemDetails.Status = StatusCodes.Status404NotFound;
                    problemDetails.Detail = notFound.Message;
                    break;

                default:
                    response.StatusCode = StatusCodes.Status500InternalServerError;
                    problemDetails.Title = "服务器内部错误";
                    problemDetails.Status = StatusCodes.Status500InternalServerError;
                    problemDetails.Detail = "处理请求时发生错误";
                    break;
            }

            var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await response.WriteAsync(json);
        }
    }
}
