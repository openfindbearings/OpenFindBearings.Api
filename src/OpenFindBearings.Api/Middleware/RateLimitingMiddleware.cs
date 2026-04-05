using OpenFindBearings.Api.Services;
using OpenFindBearings.Domain.Repositories;
using System.Collections.Concurrent;

namespace OpenFindBearings.Api.Middleware
{
    /// <summary>
    /// 限流中间件
    /// 根据用户类型限制 API 请求频率，防止滥用
    /// 限流配置从 SystemConfig 表读取，支持动态更新
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly IServiceProvider _serviceProvider;

        // 缓存限流配置，避免每次请求都查数据库
        private static Dictionary<string, int>? _cachedLimits;
        private static DateTime _cacheExpireTime;
        private static readonly SemaphoreSlim _cacheLock = new(1, 1);
        private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5); // 5分钟刷新一次

        // 存储限流记录：Key = 用户标识, Value = 请求记录列表
        private static readonly ConcurrentDictionary<string, Queue<DateTime>> _requestRecords = new();

        public RateLimitingMiddleware(
            RequestDelegate next,
            ILogger<RateLimitingMiddleware> logger,
            IServiceProvider serviceProvider)
        {
            _next = next;
            _logger = logger;
            _serviceProvider = serviceProvider;
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

            // 获取限流配置
            var limits = await GetLimitsAsync();

            // 获取用户标识
            var userKey = GetUserKey(context, currentUser);

            // 获取用户类型
            var userType = GetUserType(currentUser);

            // 获取该用户类型的限流阈值
            var limit = limits.GetValueOrDefault(userType, 30);

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
        /// 获取限流配置（带缓存）
        /// </summary>
        private async Task<Dictionary<string, int>> GetLimitsAsync()
        {
            // 检查缓存是否有效
            if (_cachedLimits != null && DateTime.UtcNow < _cacheExpireTime)
            {
                return _cachedLimits;
            }

            await _cacheLock.WaitAsync();
            try
            {
                // 双重检查
                if (_cachedLimits != null && DateTime.UtcNow < _cacheExpireTime)
                {
                    return _cachedLimits;
                }

                using var scope = _serviceProvider.CreateScope();
                var configRepo = scope.ServiceProvider.GetRequiredService<ISystemConfigRepository>();

                // 从数据库读取限流配置
                var guestLimit = await configRepo.GetValueAsync("RateLimit.Guest.RequestsPerMinute", 30);
                var userLimit = await configRepo.GetValueAsync("RateLimit.User.RequestsPerMinute", 60);
                var premiumLimit = await configRepo.GetValueAsync("RateLimit.Premium.RequestsPerMinute", 120);

                _cachedLimits = new Dictionary<string, int>
                {
                    ["guest"] = guestLimit,
                    ["user"] = userLimit,
                    ["vip"] = premiumLimit,
                    ["merchant"] = 200,
                    ["admin"] = 500
                };

                _cacheExpireTime = DateTime.UtcNow.Add(_cacheDuration);

                _logger.LogDebug("限流配置已刷新: Guest={Guest}, User={User}, Premium={Premium}",
                    guestLimit, userLimit, premiumLimit);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取限流配置失败，使用默认值");
                _cachedLimits ??= new Dictionary<string, int>
                {
                    ["guest"] = 30,
                    ["user"] = 60,
                    ["vip"] = 120,
                    ["merchant"] = 200,
                    ["admin"] = 500
                };
            }
            finally
            {
                _cacheLock.Release();
            }

            return _cachedLimits;
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

        /// <summary>
        /// 静态缓存刷新方法（供管理接口调用）
        /// </summary>
        public static void RefreshCache()
        {
            _cachedLimits = null;
            _cacheExpireTime = DateTime.MinValue;
        }
    }
}
