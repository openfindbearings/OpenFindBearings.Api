using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.DTOs;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Domain.Repositories;

namespace OpenFindBearings.Infrastructure.Services
{
    /// <summary>
    /// 价格配置提供器实现
    /// 职责：从 SystemConfigs 表读取价格相关配置（默认可见性、议价标签、数值排序开关、提取正则），并做进程内短期缓存
    /// 说明：注册为 Singleton，通过 IServiceScopeFactory 创建作用域解析 Scoped 仓储；
    ///       缓存 5 分钟以平衡实时性与查询开销，管理员在系统配置页改值后最多 5 分钟生效
    /// </summary>
    public class PriceConfigProvider : IPriceConfigProvider
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PriceConfigProvider> _logger;

        /// <summary>
        /// 缓存有效期（5 分钟）
        /// </summary>
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 缓存读取锁，避免缓存过期瞬间的并发查库击穿
        /// </summary>
        private readonly SemaphoreSlim _lock = new(1, 1);

        /// <summary>
        /// 缓存的价格配置
        /// </summary>
        private PriceConfigDto? _cache;

        /// <summary>
        /// 缓存过期时刻（UTC）
        /// </summary>
        private DateTime _cacheExpireAt = DateTime.MinValue;

        /// <summary>
        /// 创建价格配置提供器
        /// </summary>
        /// <param name="scopeFactory">服务作用域工厂，用于解析 Scoped 生命周期的仓储</param>
        /// <param name="logger">日志记录器</param>
        public PriceConfigProvider(
            IServiceScopeFactory scopeFactory,
            ILogger<PriceConfigProvider> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// 获取当前价格配置，命中缓存时直接返回
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>价格配置；读取失败时回退代码默认值</returns>
        public async Task<PriceConfigDto> GetAsync(CancellationToken cancellationToken = default)
        {
            // 缓存命中直接返回，避免批量导入时每条记录都查库
            if (_cache != null && DateTime.UtcNow < _cacheExpireAt)
                return _cache;

            await _lock.WaitAsync(cancellationToken);
            try
            {
                // 双重检查，避免并发下重复查库
                if (_cache != null && DateTime.UtcNow < _cacheExpireAt)
                    return _cache;

                var config = await LoadFromDatabaseAsync(cancellationToken);
                _cache = config;
                _cacheExpireAt = DateTime.UtcNow.Add(CacheDuration);
                return config;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// 主动失效缓存，使下一次 GetAsync 重新查库
        /// </summary>
        public void Invalidate()
        {
            // 直接清空过期标记即可，下一次读取会因缓存已过期而重新加载；
            // 此处不持锁，避免失效操作被正在进行的加载阻塞
            _cacheExpireAt = DateTime.MinValue;
        }

        /// <summary>
        /// 从 SystemConfigs 表加载价格配置
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>价格配置；出现异常时返回代码默认值</returns>
        private async Task<PriceConfigDto> LoadFromDatabaseAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<ISystemConfigRepository>();

                var defaultVisibility = await repository.GetValueAsync<string>(
                    "Price.DefaultVisibility", "LoginRequired", cancellationToken);
                var showNegotiableLabel = await repository.GetValueAsync<bool>(
                    "Price.ShowNegotiableLabel", true, cancellationToken);
                var numericForSorting = await repository.GetValueAsync<bool>(
                    "Price.NumericForSorting", true, cancellationToken);
                var extractPattern = await repository.GetValueAsync<string>(
                    "Price.ExtractPattern", @"¥(\d+(?:\.\d+)?)", cancellationToken);

                return new PriceConfigDto
                {
                    DefaultVisibility = string.IsNullOrWhiteSpace(defaultVisibility)
                        ? "LoginRequired"
                        : defaultVisibility,
                    ShowNegotiableLabel = showNegotiableLabel,
                    NumericForSorting = numericForSorting,
                    ExtractPattern = string.IsNullOrWhiteSpace(extractPattern)
                        ? @"¥(\d+(?:\.\d+)?)"
                        : extractPattern
                };
            }
            catch (Exception ex)
            {
                // 改动说明：配置读取失败（数据库不可用、配置行缺失等）不应阻断商品创建主流程，
                //           此处降级为代码默认值并记警告日志，保证 ETL 与 Excel 导入链路可用
                _logger.LogWarning(ex, "读取价格配置失败，回退使用代码默认值");
                return new PriceConfigDto();
            }
        }
    }
}
