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

        /// <summary>
        /// 配置读取失败时的短缓存时长
        /// 改动说明：原实现仅在成功路径推进过期时间，数据库不可用期间每个请求都会进锁重试查库，
        ///           形成故障期查库洪泛。此处失败后同样设一个短过期窗口，故障期最多每 30 秒重试一次
        /// </summary>
        private static readonly TimeSpan _failureCacheDuration = TimeSpan.FromSeconds(30);

        /// <summary>
        /// 单个用户标识最多保留的请求记录数，防止极端情况下内存无界增长
        /// </summary>
        private const int MaxRecordsPerKey = 1000;

        /// <summary>
        /// 用户记录队列空闲多少分钟后从字典中移除
        /// </summary>
        private const int IdleMinutesBeforeRemoval = 5;

        /// <summary>
        /// 空闲记录清扫间隔
        /// </summary>
        private static readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 上次执行空闲清扫的时刻
        /// </summary>
        private static DateTime _lastCleanupTime = DateTime.UtcNow;

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
            var whitelistPaths = new[] { "/health", "/health/ready", "/health/live", "/healthz", "/readyz" };

            if (whitelistPaths.Contains(path))
            {
                await _next(context);
                return;
            }

            // 同步接口白名单（机器间通信，不受用户级限流影响）
            if (path?.StartsWith("/api/sync/") == true)
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

            // 改动说明：合并为一次原子调用，超限时方法内部不会写入本次请求记录
            if (TryRecordRequest(userKey, limit, out var retryAfter))
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

                // 改动说明：失败路径同样推进过期时间（用较短窗口），避免数据库不可用期间
                //           每个请求都进锁重试查库造成故障期查库洪泛
                _cacheExpireTime = DateTime.UtcNow.Add(_failureCacheDuration);
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
            // 优先级：ClientId > UserId > SessionId > IP
            // 改动说明：同步客户端（sync-client）没有业务用户ID，若落到 IP 维度会与同 NAT 出口的
            //           匿名流量共享配额。ClientId 由 UserContextMiddleware 写入，优先用它计数
            if (context.Items.TryGetValue("ClientId", out var clientId) && clientId is string cid && !string.IsNullOrEmpty(cid))
                return $"client_{cid}";

            if (currentUser.UserId.HasValue)
                return $"user_{currentUser.UserId.Value}";

            if (!string.IsNullOrEmpty(currentUser.SessionId))
                return $"session_{currentUser.SessionId}";

            return $"ip_{currentUser.ClientIp ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
        }

        /// <summary>
        /// 获取用户类型
        /// 改动说明：原实现在此处把"角色名"映射为限流档位，但 UserType 枚举与 User 实体字段移除后
        ///           上游不再写入该值，导致所有登录用户恒被判为 guest。
        ///           现在角色到档位的映射已上移到 UserContextMiddleware（依据 RBAC 角色推导），
        ///           此处只保留未登录判定与兜底，直接透传上游结果
        /// </summary>
        /// <param name="currentUser">当前用户服务</param>
        /// <returns>限流分档标识</returns>
        private string GetUserType(ICurrentUserService currentUser)
        {
            if (!currentUser.IsAuthenticated)
                return RateLimitUserType.Guest;

            return currentUser.UserType ?? RateLimitUserType.User;
        }

        /// <summary>
        /// 尝试记录一次请求，并判断是否已超出限流阈值
        /// </summary>
        /// <param name="key">用户唯一标识</param>
        /// <param name="limit">该用户类型每分钟允许的请求数</param>
        /// <param name="retryAfter">超出限制时需要等待的秒数</param>
        /// <returns>true 表示已超出限制，请求应被拒绝；false 表示已记录本次请求，可继续处理</returns>
        private bool TryRecordRequest(string key, int limit, out int retryAfter)
        {
            retryAfter = 0;

            // 改动说明：空闲清扫先于 limit 判定执行。原实现在不限制的路径上提前返回，
            //           使被豁免的用户标识永久占据字典条目，无法回收
            CleanupIdleRecords();

            // limit 小于等于 0 视为不限制
            if (limit <= 0)
                return false;

            // 改动说明：滑动窗口的"读取 + 清理 + 写入"必须在同一把锁内完成。
            //           原实现拆成两个方法，读取侧无锁而写入侧加锁，Queue&lt;T&gt; 非线程安全，
            //           并发请求同时 Dequeue 与 Enqueue 会破坏内部状态，可能抛异常导致 500
            var records = _requestRecords.GetOrAdd(key, _ => new Queue<DateTime>());
            lock (records)
            {
                // 清理超过 1 分钟的旧记录
                var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
                while (records.Count > 0 && records.Peek() < oneMinuteAgo)
                {
                    records.Dequeue();
                }

                if (records.Count >= limit)
                {
                    var oldestRecord = records.Peek();
                    retryAfter = (int)Math.Ceiling((oldestRecord.AddMinutes(1) - DateTime.UtcNow).TotalSeconds);
                    if (retryAfter < 1) retryAfter = 1;
                    return true;
                }

                records.Enqueue(DateTime.UtcNow);

                // 限制单个队列长度，防止极端情况下内存无界增长
                while (records.Count > MaxRecordsPerKey)
                {
                    records.Dequeue();
                }

                return false;
            }
        }

        /// <summary>
        /// 清扫长时间无请求的用户记录队列
        /// 改动说明：原实现的记录字典只增不减，每个出现过的 IP 或用户永久占用一个队列条目，
        ///           长期运行内存单调增长。此处按固定间隔移除空闲超过阈值的条目
        /// </summary>
        private static void CleanupIdleRecords()
        {
            var now = DateTime.UtcNow;
            if (now - _lastCleanupTime < _cleanupInterval)
                return;

            _lastCleanupTime = now;
            var idleThreshold = now.AddMinutes(-IdleMinutesBeforeRemoval);

            foreach (var entry in _requestRecords)
            {
                lock (entry.Value)
                {
                    // 队列为空，或最后一条记录已超过空闲阈值，则该条目可回收
                    if (entry.Value.Count == 0 || entry.Value.LastOrDefault() < idleThreshold)
                    {
                        _requestRecords.TryRemove(entry.Key, out _);
                    }
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
