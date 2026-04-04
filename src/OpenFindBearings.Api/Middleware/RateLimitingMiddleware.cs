using OpenFindBearings.Api.Services;
using System.Collections.Concurrent;

namespace OpenFindBearings.Api.Middleware
{
    /// <summary>
    /// 限流中间件
    /// 根据用户类型限制 API 请求频率，防止滥用
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;

        // 存储限流记录：Key = 用户标识, Value = 请求记录列表
        private static readonly ConcurrentDictionary<string, Queue<DateTime>> _requestRecords = new();

        // 各用户类型的限流配置（每分钟最大请求数）
        private static readonly Dictionary<string, int> _limits = new()
        {
            //["guest"] = 10,      // 游客：10次/分钟
            //["user"] = 30,       // 普通用户：30次/分钟
            //["vip"] = 100,       // VIP用户：100次/分钟
            //["merchant"] = 200,  // 商家：200次/分钟
            //["admin"] = 500      // 管理员：500次/分钟

            ["guest"] = 60,      // 游客：60 次/分钟
            ["user"] = 120,      // 普通用户：120 次/分钟
            ["vip"] = 300,       // VIP用户：300 次/分钟
            ["merchant"] = 500,  // 商家：500 次/分钟
            ["admin"] = 1000     // 管理员：1000 次/分钟
        };

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUser)
        {
            // 健康检查路径白名单（不限流）
            var path = context.Request.Path.Value?.ToLower();
            var whitelistPaths = new[] { "/health", "/ready", "/live", "/healthz", "/readyz" };

            if (whitelistPaths.Contains(path))
            {
                await _next(context);
                return;
            }

            // 获取用户标识
            var userKey = GetUserKey(context, currentUser);

            // 获取用户类型
            var userType = GetUserType(currentUser);

            // 获取该用户类型的限流阈值
            var limit = _limits.GetValueOrDefault(userType, 30);

            // 检查是否超过限流
            if (IsRateLimitExceeded(userKey, limit, out var retryAfter))
            {
                _logger.LogWarning("请求被限流: UserKey={UserKey}, UserType={UserType}, Limit={Limit}/min",
                    userKey, userType, limit);

                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers.RetryAfter = retryAfter.ToString();

                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    code = 429,
                    message = $"请求过于频繁，请 {retryAfter} 秒后再试",
                    retryAfter = retryAfter
                });
                return;
            }

            // 记录本次请求
            AddRequestRecord(userKey);

            await _next(context);
        }

        /// <summary>
        /// 获取用户唯一标识
        /// </summary>
        private string GetUserKey(HttpContext context, ICurrentUserService currentUser)
        {
            // 优先级：UserId > SessionId > IP
            if (currentUser.UserId.HasValue)
                return $"user_{currentUser.UserId.Value}";

            if (!string.IsNullOrEmpty(currentUser.SessionId))
                return $"session_{currentUser.SessionId}";

            return $"ip_{currentUser.ClientIp ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
        }

        /// <summary>
        /// 获取用户类型
        /// </summary>
        private string GetUserType(ICurrentUserService currentUser)
        {
            if (currentUser.IsAuthenticated)
            {
                // 根据用户类型返回限流级别
                var userType = currentUser.UserType?.ToLower();

                return userType switch
                {
                    "admin" => "admin",
                    "merchantstaff" => "merchant",
                    var x when x != null && x.Contains("vip") => "vip",
                    _ => "user"
                };
            }

            return "guest";
        }

        /// <summary>
        /// 检查是否超过限流
        /// </summary>
        private bool IsRateLimitExceeded(string key, int limit, out int retryAfter)
        {
            retryAfter = 0;

            if (!_requestRecords.TryGetValue(key, out var records))
                return false;

            // 清理超过1分钟的旧记录
            var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
            while (records.Count > 0 && records.Peek() < oneMinuteAgo)
            {
                records.Dequeue();
            }

            // 检查是否超过限制
            if (records.Count >= limit)
            {
                // 计算需要等待的秒数
                var oldestRecord = records.Peek();
                retryAfter = (int)Math.Ceiling((oldestRecord.AddMinutes(1) - DateTime.UtcNow).TotalSeconds);
                if (retryAfter < 1) retryAfter = 1;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 添加请求记录
        /// </summary>
        private void AddRequestRecord(string key)
        {
            var records = _requestRecords.GetOrAdd(key, _ => new Queue<DateTime>());
            lock (records)
            {
                records.Enqueue(DateTime.UtcNow);

                // 限制队列大小，防止内存无限增长
                while (records.Count > 1000)
                {
                    records.Dequeue();
                }
            }
        }
    }
}
