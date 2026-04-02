using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenFindBearings.Api.Helpers;
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

                // ============ 数据库异常处理 ============

                case DbUpdateConcurrencyException concurrencyException:
                    response.StatusCode = StatusCodes.Status409Conflict;
                    problemDetails.Title = "数据冲突";
                    problemDetails.Status = StatusCodes.Status409Conflict;
                    problemDetails.Detail = "数据已被其他用户修改，请刷新后重试";
                    problemDetails.Extensions["affected_entities"] = concurrencyException.Entries
                        .Select(e => e.Entity.GetType().Name);
                    break;

                case DbUpdateException dbUpdateException:
                    response.StatusCode = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "数据操作失败";
                    problemDetails.Status = StatusCodes.Status400BadRequest;

                    // 检查是否为唯一约束冲突
                    if (IsUniqueConstraintViolation(dbUpdateException))
                    {
                        problemDetails.Detail = "数据已存在，请勿重复添加";
                        problemDetails.Title = "数据重复";
                        problemDetails.Extensions["duplicate"] = true;
                    }
                    else
                    {
                        problemDetails.Detail = "保存数据时发生错误";
                    }

                    // 可选：记录详细的数据库错误信息（但不返回给客户端）
                    _logger.LogWarning(dbUpdateException, "数据库操作失败");
                    break;

                // PostgreSQL 特定异常（如果使用 Npgsql）
                case PostgresException postgresException:
                    response.StatusCode = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "数据库错误";
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Detail = PostgreSqlErrorHandler.GetShortFriendlyMessage(postgresException);
                    problemDetails.Extensions["sql_state"] = postgresException.SqlState;
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

        /// <summary>
        /// 判断异常是否为唯一约束冲突
        /// </summary>
        private bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            // 检查内部异常
            var innerException = ex.InnerException;

            // PostgreSQL (Npgsql)
            if (innerException is PostgresException pgEx && pgEx.SqlState == "23505")
                return true;

            // 检查异常消息（备选方案）
            if (innerException?.Message?.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true)
                return true;

            return false;
        }
    }
}
