using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenFindBearings.Application.Common.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace OpenFindBearings.Infrastructure.Services
{
    /// <summary>
    /// 缓存服务配置
    /// </summary>
    public class CacheSettings
    {
        /// <summary>
        /// 是否启用Redis
        /// </summary>
        public bool EnableRedis { get; set; } = false;

        /// <summary>
        /// Redis连接字符串
        /// </summary>
        public string? RedisConnectionString { get; set; }

        /// <summary>
        /// 默认过期时间（分钟）
        /// </summary>
        public int DefaultExpirationMinutes { get; set; } = 60;

        /// <summary>
        /// 内存缓存大小限制（MB）
        /// </summary>
        public int MemoryCacheSizeLimit { get; set; } = 100;

        /// <summary>
        /// 是否启用缓存统计
        /// </summary>
        public bool EnableStatistics { get; set; } = false;
    }

    /// <summary>
    /// 混合缓存服务实现
    /// 支持内存缓存和Redis分布式缓存
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IConnectionMultiplexer? _redisConnection;
        private readonly ILogger<CacheService> _logger;
        private readonly CacheSettings _settings;
        private readonly JsonSerializerOptions _jsonOptions;

        // Redis数据库
        private StackExchange.Redis.IDatabase? _redisDb;

        // 统计信息
        private int _hitCount;
        private int _missCount;
        private readonly object _statsLock = new();

        /// <summary>
        /// 构造函数
        /// </summary>
        public CacheService(
            IMemoryCache memoryCache,
            ILogger<CacheService> logger,
            IOptions<CacheSettings> settings,
            IConnectionMultiplexer? redisConnection = null)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _settings = settings.Value;
            _redisConnection = redisConnection;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            if (_redisConnection != null && _redisConnection.IsConnected)
            {
                _redisDb = _redisConnection.GetDatabase();
                _logger.LogInformation("Redis缓存已连接");
            }
            else
            {
                _logger.LogWarning("Redis未连接，仅使用内存缓存");
            }
        }

        /// <summary>
        /// 从缓存获取数据
        /// 策略：先查内存，再查Redis，都没有则返回null
        /// </summary>
        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. 先查内存缓存（最快）
                if (_memoryCache.TryGetValue(key, out T? memoryValue))
                {
                    RecordHit();
                    _logger.LogDebug("内存缓存命中: {Key}", key);
                    return memoryValue;
                }

                // 2. 如果启用了Redis，查Redis
                if (_settings.EnableRedis && _redisDb != null)
                {
                    var redisValue = await _redisDb.StringGetAsync(key);
                    if (redisValue.HasValue)
                    {
                        RecordHit();
                        _logger.LogDebug("Redis缓存命中: {Key}", key);

                        // 反序列化
                        var value = JsonSerializer.Deserialize<T>(redisValue!.ToString(), _jsonOptions);

                        // 同时存入内存缓存（下次更快）
                        if (value != null)
                        {
                            var memoryOptions = GetMemoryEntryOptions(key);
                            _memoryCache.Set(key, value, memoryOptions);
                        }

                        return value;
                    }
                }

                RecordMiss();
                _logger.LogDebug("缓存未命中: {Key}", key);
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取缓存失败: {Key}", key);
                return default;
            }
        }

        /// <summary>
        /// 设置缓存数据
        /// 策略：同时写入内存和Redis（如果启用）
        /// </summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. 写入内存缓存
                var memoryOptions = GetMemoryEntryOptions(key, expiration);
                _memoryCache.Set(key, value, memoryOptions);
                _logger.LogDebug("内存缓存已设置: {Key}, 过期时间: {Expiration}", key, expiration);

                // 2. 如果启用了Redis，写入Redis
                if (_settings.EnableRedis && _redisDb != null)
                {
                    var json = JsonSerializer.Serialize(value, _jsonOptions);
                    await _redisDb.StringSetAsync(key, json, expiration);
                    _logger.LogDebug("Redis缓存已设置: {Key}", key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "设置缓存失败: {Key}", key);
            }
        }

        /// <summary>
        /// 移除缓存
        /// 策略：同时从内存和Redis移除
        /// </summary>
        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. 移除内存缓存
                _memoryCache.Remove(key);
                _logger.LogDebug("内存缓存已移除: {Key}", key);

                // 2. 如果启用了Redis，移除Redis
                if (_settings.EnableRedis && _redisDb != null)
                {
                    await _redisDb.KeyDeleteAsync(key);
                    _logger.LogDebug("Redis缓存已移除: {Key}", key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "移除缓存失败: {Key}", key);
            }
        }

        /// <summary>
        /// 检查缓存是否存在
        /// </summary>
        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. 检查内存
                if (_memoryCache.TryGetValue(key, out _))
                    return true;

                // 2. 如果启用了Redis，检查Redis
                if (_settings.EnableRedis && _redisDb != null)
                {
                    return await _redisDb.KeyExistsAsync(key);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "检查缓存失败: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// 批量获取缓存（Redis特性）
        /// </summary>
        public async Task<IDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<string, T?>();

            if (!_settings.EnableRedis || _redisDb == null)
            {
                // 如果不支持Redis，逐个从内存获取
                foreach (var key in keys)
                {
                    result[key] = await GetAsync<T>(key, cancellationToken);
                }
                return result;
            }

            try
            {
                // Redis批量获取（高性能）
                var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
                var redisValues = await _redisDb.StringGetAsync(redisKeys);

                for (int i = 0; i < keys.Count(); i++)
                {
                    var key = keys.ElementAt(i);
                    var value = redisValues[i];

                    if (value.HasValue)
                    {
                        var deserialized = JsonSerializer.Deserialize<T>(value!.ToString(), _jsonOptions);
                        result[key] = deserialized;

                        // 同步到内存缓存
                        if (deserialized != null)
                        {
                            _memoryCache.Set(key, deserialized, GetMemoryEntryOptions(key));
                        }
                    }
                    else
                    {
                        result[key] = default;
                    }
                }

                _logger.LogDebug("Redis批量获取完成，获取到 {Count} 个缓存", result.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis批量获取失败，降级为逐个获取");
                // 降级：逐个获取
                foreach (var key in keys)
                {
                    result[key] = await GetAsync<T>(key, cancellationToken);
                }
            }

            return result;
        }

        /// <summary>
        /// 获取内存缓存选项
        /// </summary>
        private MemoryCacheEntryOptions GetMemoryEntryOptions(string key, TimeSpan? expiration = null)
        {
            var options = new MemoryCacheEntryOptions();

            // 设置绝对过期时间
            if (expiration.HasValue)
            {
                options.SetAbsoluteExpiration(expiration.Value);
            }
            else
            {
                // 默认滑动过期（10分钟无访问则清除）
                options.SetSlidingExpiration(TimeSpan.FromMinutes(10));
            }

            // 注册缓存移除回调
            options.RegisterPostEvictionCallback((callbackKey, value, reason, state) =>
            {
                _logger.LogDebug("内存缓存被移除: {Key}, 原因: {Reason}", callbackKey, reason);
            });

            // 设置优先级（防止内存不足时被优先清除）
            options.SetPriority(CacheItemPriority.Normal);

            // 设置大小（用于限制缓存总大小）
            options.SetSize(1);

            return options;
        }

        /// <summary>
        /// 记录缓存命中
        /// </summary>
        private void RecordHit()
        {
            if (_settings.EnableStatistics)
            {
                lock (_statsLock)
                {
                    _hitCount++;
                }
            }
        }

        /// <summary>
        /// 记录缓存未命中
        /// </summary>
        private void RecordMiss()
        {
            if (_settings.EnableStatistics)
            {
                lock (_statsLock)
                {
                    _missCount++;
                }
            }
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            // 修复：IMemoryCache 没有 Count 属性，改用其他方式
            var memoryCache = _memoryCache as MemoryCache;

            return new CacheStatistics
            {
                HitCount = _hitCount,
                MissCount = _missCount,
                HitRate = _hitCount + _missCount > 0
                    ? (double)_hitCount / (_hitCount + _missCount)
                    : 0,
                MemoryCacheCount = memoryCache?.Count ?? 0  // MemoryCache 有 Count 属性
            };
        }

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // 内存缓存无法批量清除，只能重新创建
                if (_memoryCache is MemoryCache memoryCache)
                {
                    memoryCache.Compact(1.0);
                }

                // 如果启用了Redis，清空Redis（谨慎使用！）
                if (_settings.EnableRedis && _redisDb != null)
                {
                    var endpoints = _redisConnection?.GetEndPoints();
                    if (endpoints != null)
                    {
                        foreach (var endpoint in endpoints)
                        {
                            var server = _redisConnection?.GetServer(endpoint);
                            if (server != null)
                            {
                                await server.FlushDatabaseAsync();
                            }
                        }
                    }
                }

                _logger.LogInformation("所有缓存已清除");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除缓存失败");
            }
        }
    }

    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public class CacheStatistics
    {
        /// <summary>
        /// 命中次数
        /// </summary>
        public int HitCount { get; set; }

        /// <summary>
        /// 未命中次数
        /// </summary>
        public int MissCount { get; set; }

        /// <summary>
        /// 命中率
        /// </summary>
        public double HitRate { get; set; }

        /// <summary>
        /// 内存缓存项数量
        /// </summary>
        public int MemoryCacheCount { get; set; }
    }
}
