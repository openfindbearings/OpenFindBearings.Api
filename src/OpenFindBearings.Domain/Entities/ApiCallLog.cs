using OpenFindBearings.Domain.Abstractions;

namespace OpenFindBearings.Domain.Entities
{
    /// <summary>
    /// API调用日志（用于限流和监控）
    /// </summary>
    public class ApiCallLog : BaseEntity
    {
        /// <summary>
        /// 用户ID（可为空）
        /// </summary>
        public Guid? UserId { get; private set; }

        /// <summary>
        /// 游客会话ID
        /// </summary>
        public string? SessionId { get; private set; }

        /// <summary>
        /// API路径
        /// </summary>
        public string ApiPath { get; private set; } = string.Empty;

        /// <summary>
        /// HTTP方法
        /// </summary>
        public string HttpMethod { get; private set; } = string.Empty;

        /// <summary>
        /// 响应状态码
        /// </summary>
        public int StatusCode { get; private set; }

        /// <summary>
        /// 请求耗时（毫秒）
        /// </summary>
        public int DurationMs { get; private set; }

        /// <summary>
        /// 客户端IP
        /// </summary>
        public string? ClientIp { get; private set; }

        /// <summary>
        /// 用户代理
        /// </summary>
        public string? UserAgent { get; private set; }

        private ApiCallLog() { }

        public ApiCallLog(
            Guid? userId,
            string? sessionId,
            string apiPath,
            string httpMethod,
            int statusCode,
            int durationMs,
            string? clientIp = null,
            string? userAgent = null)
        {
            UserId = userId;
            SessionId = sessionId;
            ApiPath = apiPath;
            HttpMethod = httpMethod;
            StatusCode = statusCode;
            DurationMs = durationMs;
            ClientIp = clientIp;
            UserAgent = userAgent;
        }
    }
}
