namespace OpenFindBearings.Api.Helpers
{
    /// <summary>
    /// HttpContext扩展方法
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// 获取当前用户ID（业务系统ID）
        /// </summary>
        public static Guid? GetUserId(this HttpContext httpContext)
        {
            return httpContext.Items["UserId"] as Guid?;
        }

        /// <summary>
        /// 获取当前用户认证ID
        /// </summary>
        public static string? GetAuthUserId(this HttpContext httpContext)
        {
            return httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// 获取会话ID（游客）
        /// </summary>
        public static string? GetSessionId(this HttpContext httpContext)
        {
            return httpContext.Items["SessionId"] as string;
        }

        /// <summary>
        /// 获取客户端IP
        /// </summary>
        public static string? GetClientIp(this HttpContext httpContext)
        {
            return httpContext.Connection.RemoteIpAddress?.ToString();
        }

        /// <summary>
        /// 获取User-Agent
        /// </summary>
        public static string? GetUserAgent(this HttpContext httpContext)
        {
            return httpContext.Request.Headers["User-Agent"].FirstOrDefault();
        }

        /// <summary>
        /// 判断是否是移动端请求
        /// </summary>
        public static bool IsMobile(this HttpContext httpContext)
        {
            var userAgent = httpContext.GetUserAgent()?.ToLower();
            if (string.IsNullOrEmpty(userAgent))
                return false;

            return userAgent.Contains("mobile") ||
                   userAgent.Contains("android") ||
                   userAgent.Contains("iphone") ||
                   userAgent.Contains("ios") ||
                   userAgent.Contains("ipad");
        }

        /// <summary>
        /// 获取分页参数
        /// </summary>
        public static (int Page, int PageSize) GetPagingParams(this HttpContext httpContext,
            int defaultPage = 1, int defaultPageSize = 20, int maxPageSize = 100)
        {
            var page = httpContext.Request.Query.TryGetValue("page", out var pageValue)
                && int.TryParse(pageValue, out var p) ? p : defaultPage;

            var pageSize = httpContext.Request.Query.TryGetValue("pageSize", out var sizeValue)
                && int.TryParse(sizeValue, out var ps) ? Math.Min(ps, maxPageSize) : defaultPageSize;

            return (Math.Max(page, 1), Math.Max(pageSize, 1));
        }
    }
}
