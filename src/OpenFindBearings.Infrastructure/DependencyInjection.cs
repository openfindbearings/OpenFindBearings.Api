using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Interfaces;
using OpenFindBearings.Domain.Interfaces;
using OpenFindBearings.Infrastructure.Persistence.Data;
using OpenFindBearings.Infrastructure.Persistence.Repositories;
using OpenFindBearings.Infrastructure.Services;
using StackExchange.Redis;

namespace OpenFindBearings.Infrastructure
{
    /// <summary>
    /// 基础设施层依赖注入配置
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ============ 1. 添加DbContext ============
            var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isDev = string.IsNullOrWhiteSpace(envName) || envName == "Development"; // 默认视为开发环境

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                if (isDev)
                {
                    options.UseSqlite(configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
                }
                else
                {
                    // 生产环境：PostgreSQL
                    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
                }
            });
            

            // ============ 2. 注册所有仓储 ============

            // 核心业务仓储
            services.AddScoped<IBearingRepository, BearingRepository>();
            services.AddScoped<IMerchantRepository, MerchantRepository>();
            services.AddScoped<IMerchantBearingRepository, MerchantBearingRepository>();
            services.AddScoped<IBearingInterchangeRepository, BearingInterchangeRepository>();
            services.AddScoped<ICorrectionRequestRepository, CorrectionRequestRepository>();
            services.AddScoped<ILicenseVerificationRepository, LicenseVerificationRepository>();

            // 品牌和类型字典仓储
            services.AddScoped<IBrandRepository, BrandRepository>();
            services.AddScoped<IBearingTypeRepository, BearingTypeRepository>();

            // 用户相关仓储
            services.AddScoped<IUserRepository, UserRepository>();

            // 用户收藏与历史仓储
            services.AddScoped<IUserBearingFavoriteRepository, UserBearingFavoriteRepository>();
            services.AddScoped<IUserMerchantFollowRepository, UserMerchantFollowRepository>();
            services.AddScoped<IUserBearingHistoryRepository, UserBearingHistoryRepository>();
            services.AddScoped<IUserMerchantHistoryRepository, UserMerchantHistoryRepository>();

            // 权限相关仓储
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();
            services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();

            // 审核日志和系统配置仓储
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();

            // 邀请仓储
            services.AddScoped<IStaffInvitationRepository, StaffInvitationRepository>();

            // ============ 3. 注册缓存服务 ============

            // 读取缓存配置
            var cacheSettings = configuration.GetSection("CacheSettings").Get<CacheSettings>()
                ?? new CacheSettings();

            // 添加内存缓存
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = cacheSettings.MemoryCacheSizeLimit * 1024 * 1024;
                options.ExpirationScanFrequency = TimeSpan.FromMinutes(1);
            });

            // 如果启用Redis，注册Redis连接
            if (cacheSettings.EnableRedis && !string.IsNullOrEmpty(cacheSettings.RedisConnectionString))
            {
                services.AddSingleton<IConnectionMultiplexer>(sp =>
                {
                    var config = ConfigurationOptions.Parse(cacheSettings.RedisConnectionString);
                    config.AbortOnConnectFail = false;
                    config.ConnectTimeout = 5000;

                    var logger = sp.GetRequiredService<ILogger<IConnectionMultiplexer>>();

                    try
                    {
                        var connection = ConnectionMultiplexer.Connect(config);
                        connection.ConnectionFailed += (sender, e) =>
                        {
                            logger.LogWarning(e.Exception, "Redis连接失败: {EndPoint}", e.EndPoint);
                        };
                        connection.ConnectionRestored += (sender, e) =>
                        {
                            logger.LogInformation("Redis连接恢复: {EndPoint}", e.EndPoint);
                        };

                        logger.LogInformation("Redis连接成功");
                        return connection;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Redis连接失败，将降级为内存缓存");
                        return null!;
                    }
                });
            }

            // 注册缓存服务
            services.AddSingleton<ICacheService, CacheService>();

            // ============ 4. 注册认证服务客户端 ============
            var identityBaseUrl = configuration["Authentication:Authority"] ?? "https://localhost:7201";

            services.AddHttpClient<IIdentityService, IdentityService>(client =>
            {
                client.BaseAddress = new Uri(identityBaseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // ============ 5. 注册其他基础设施服务 ============

            // 通知服务
            services.AddScoped<INotificationService, NotificationService>();

            // 轴承统计服务
            services.AddScoped<IBearingViewStatsService, BearingViewStatsService>();

            return services;
        }
    }
}
