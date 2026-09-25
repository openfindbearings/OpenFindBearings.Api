using Microsoft.EntityFrameworkCore;
using Amazon.S3;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenFindBearings.Application.Services;
using OpenFindBearings.Application.Shared.Interfaces;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence;
using OpenFindBearings.Infrastructure.Persistence.Data;
using OpenFindBearings.Infrastructure.Persistence.Repositories;
using OpenFindBearings.Infrastructure.Services;
using OpenFindBearings.Infrastructure.Services.ObjectStorage;
using StackExchange.Redis;

namespace OpenFindBearings.Infrastructure
{
    /// <summary>
    /// 基础设施层依赖注入配置
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // ============ 1. 添加DbContext ============
            // 连接串 Timezone=UTC 确保 Npgsql 读写均为 UTC，无需自定义拦截器
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            });

            // 注册 UnitOfWork
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // ============ 2. 注册所有仓储 ============

            // 核心业务仓储
            services.AddScoped<IBearingRepository, BearingRepository>();
            services.AddScoped<IMerchantRepository, MerchantRepository>();
            services.AddScoped<IMerchantBearingRepository, MerchantBearingRepository>();
            services.AddScoped<IBearingInterchangeRepository, BearingInterchangeRepository>();
            services.AddScoped<ICorrectionRequestRepository, CorrectionRequestRepository>();
            services.AddScoped<IMerchantDocumentRepository, MerchantDocumentRepository>();

            // 品牌和类型字典仓储
            services.AddScoped<IBrandRepository, BrandRepository>();
            services.AddScoped<IBearingTypeRepository, BearingTypeRepository>();

            // 用户相关仓储
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserPreferenceRepository, UserPreferenceRepository>();

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

            // 商户成员仓储（一人多商户的唯一事实源）
            services.AddScoped<IMerchantMemberRepository, MerchantMemberRepository>();

        // 改动说明：站内信仓储（通知事件订阅者与消息中心端点共用）
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // v1.32.0 积分底座：账户/流水/规则仓储
        services.AddScoped<IPointAccountRepository, PointAccountRepository>();
        services.AddScoped<IPointTransactionRepository, PointTransactionRepository>();
        services.AddScoped<IPointGrantRuleRepository, PointGrantRuleRepository>();
        // v1.34.0：一次性奖励认领台账（号/照维度防刷，永不随注销删除）
        services.AddScoped<IPointRewardClaimRepository, PointRewardClaimRepository>();

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

        // v1.32.0 积分底座：唯一写入口服务
        services.AddScoped<IPointsService, PointsService>();

            // 轴承统计服务
            services.AddScoped<IBearingViewStatsService, BearingViewStatsService>();

            // 改动说明：新增价格配置提供器，供商品创建与搜索排序处理器消费 Price.* 系统配置项
            // 注册为 Singleton 以启用进程内 5 分钟缓存，内部通过 IServiceScopeFactory 解析 Scoped 仓储
            services.AddSingleton<IPriceConfigProvider, PriceConfigProvider>();

            // ========== 对象存储 ==========
            // 改动说明（v1.5.0）：用户上传（头像/Logo/证照材料）从端点直写本地盘改为走 IObjectStorageService
            // 抽象；生产 Provider=Minio（S3 协议对接 MinIO，ForcePathStyle 为自建 MinIO 必需），
            // 开发默认 LocalFile（写 wwwroot，media 同源直出）。原 IFileService/LocalFileService 死代码删除。
            var storageProvider = configuration["FileStorage:Provider"] ?? "LocalFile";
            switch (storageProvider)
            {
                case "Minio":
                    services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
                        configuration["FileStorage:AccessKey"] ?? "minioadmin",
                        configuration["FileStorage:SecretKey"] ?? "minioadmin",
                        new Amazon.S3.AmazonS3Config
                        {
                            ServiceURL = configuration["FileStorage:Endpoint"] ?? "http://minio:9000",
                            ForcePathStyle = true,
                            AuthenticationRegion = "us-east-1"
                        }));
                    services.AddScoped<IObjectStorageService, MinioStorage>();
                    break;
                case "LocalFile":
                    services.AddScoped<IObjectStorageService, LocalFileStorage>();
                    break;
                default:
                    throw new NotSupportedException($"不支持的对象存储类型: {storageProvider}");
            }

            // 添加后台任务队列服务
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHostedService<QueuedHostedService>();
        // v2.12.0：注销冷静期到期匿名化 Job（每小时扫描，注销满 30 天清 PII）
        services.AddHostedService<UserDeactivationJob>();

            return services;
        }
    }
}
