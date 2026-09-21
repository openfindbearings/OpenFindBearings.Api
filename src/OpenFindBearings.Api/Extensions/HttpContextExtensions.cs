namespace OpenFindBearings.Api.Extensions
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
        /// 获取当前用户手机号（JWT phone_number claim，OIDC 标准声明）。
        /// 改动说明：v2.9.0 成员邀请确认制需要服务端权威比对"被邀手机号==登录者手机号"，
        ///   不接受客户端自报参数，防拿他人手机号撞领邀请。
        /// </summary>
        public static string? GetPhone(this HttpContext httpContext)
        {
            return httpContext.User?.FindFirst("phone_number")?.Value;
        }

        /// <summary>
        /// 获取当前用户邮箱（JWT email claim，OIDC 标准声明）。
        /// 改动说明（v2.9.0）：邮箱注册无手机号的被邀人，员工邀请按 email 兜底匹配。
        /// </summary>
        public static string? GetEmail(this HttpContext httpContext)
        {
            return httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                ?? httpContext.User?.FindFirst("email")?.Value;
        }

        /// <summary>
        /// 获取当前用户租户ID（从 JWT token claims 中读取 tenant_id）
        /// </summary>
        public static Guid? GetTenantId(this HttpContext httpContext)
        {
            var claim = httpContext.User?.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(claim, out var tenantId) ? tenantId : null;
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
