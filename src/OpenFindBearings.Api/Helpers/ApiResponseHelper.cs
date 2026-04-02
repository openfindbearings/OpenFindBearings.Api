using OpenFindBearings.Api.DTOs.Responses;

namespace OpenFindBearings.Api.Helpers
{
    /// <summary>
    /// API响应辅助类
    /// </summary>
    public static class ApiResponseHelper
    {
        /// <summary>
        /// 成功响应（带数据）
        /// </summary>
        public static IResult Ok<T>(T data, string? message = null, HttpContext? httpContext = null)
        {
            var response = new ApiResponse<T>
            {
                Success = true,
                Code = 200,
                Data = data,
                Message = message,
                TraceId = httpContext?.TraceIdentifier
            };
            return TypedResults.Ok(response);
        }

        /// <summary>
        /// 成功响应（无数据）
        /// </summary>
        public static IResult Ok(string? message = null, HttpContext? httpContext = null)
        {
            var response = new ApiResponse<object>
            {
                Success = true,
                Code = 200,
                Data = null,
                Message = message,
                TraceId = httpContext?.TraceIdentifier
            };
            return TypedResults.Ok(response);
        }

        /// <summary>
        /// 创建成功响应（201 Created）
        /// </summary>
        public static IResult Created<T>(string uri, T data, string? message = null, HttpContext? httpContext = null)
        {
            var response = new ApiResponse<T>
            {
                Success = true,
                Code = 201,
                Data = data,
                Message = message,
                TraceId = httpContext?.TraceIdentifier
            };
            return TypedResults.Created(uri, response);
        }

        /// <summary>
        /// 错误响应（400 Bad Request）
        /// </summary>
        public static IResult BadRequest(string message, Dictionary<string, string[]>? errors = null, HttpContext? httpContext = null)
        {
            var response = new ApiResponse<object>
            {
                Success = false,
                Code = 400,
                Message = message,
                Errors = errors,
                TraceId = httpContext?.TraceIdentifier
            };
            return TypedResults.BadRequest(response);
        }

        /// <summary>
        /// 未授权响应（401 Unauthorized）
        /// </summary>
        public static IResult Unauthorized(string? message = null, HttpContext? httpContext = null)
        {
            var response = new ApiResponse<object>
            {
                Success = false,
                Code = 401,
                Message = message ?? "未授权，请先登录",
                TraceId = httpContext?.TraceIdentifier
            };
            return TypedResults.Unauthorized();
        }

        /// <summary>
        /// 禁止访问响应（403 Forbidden）
        /// </summary>
        public static IResult Forbidden(string? message = null, HttpContext? httpContext = null)
        {
            var response = new ApiResponse<object>
            {
                Success = false,
                Code = 403,
                Message = message ?? "没有权限访问",
                TraceId = httpContext?.TraceIdentifier
            };
            return TypedResults.Forbid();
        }

        /// <summary>
        /// 资源不存在响应（404 Not Found）
        /// </summary>
        public static IResult NotFound(string? message = null, HttpContext? httpContext = null)
        {
            var response = new ApiResponse<object>
            {
                Success = false,
                Code = 404,
                Message = message ?? "资源不存在",
                TraceId = httpContext?.TraceIdentifier
            };
            return TypedResults.NotFound(response);
        }

        /// <summary>
        /// 服务器错误响应（500 Internal Server Error）
        /// </summary>
        public static IResult Problem(string? title = null, string? detail = null, HttpContext? httpContext = null)
        {
            var response = new ApiResponse<object>
            {
                Success = false,
                Code = 500,
                Message = title ?? "服务器内部错误",
                TraceId = httpContext?.TraceIdentifier
            };
            return TypedResults.Problem(
                title: title,
                detail: detail,
                statusCode: 500,
                extensions: new Dictionary<string, object?>
                {
                    ["response"] = response
                }
            );
        }

        /// <summary>
        /// 分页响应
        /// </summary>
        public static IResult Paged<T>(List<T> items, int totalCount, int page, int pageSize, HttpContext? httpContext = null)
        {
            var pagedData = new PagedResponse<T>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            var response = new ApiResponse<PagedResponse<T>>
            {
                Success = true,
                Code = 200,
                Data = pagedData,
                TraceId = httpContext?.TraceIdentifier
            };
            return TypedResults.Ok(response);
        }
    }
}
