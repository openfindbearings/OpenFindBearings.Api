using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenFindBearings.Api.Services;
using OpenFindBearings.Application.Interfaces;
using OpenFindBearings.Domain.Interfaces;
using OpenFindBearings.Infrastructure.Persistence.Data;
using OpenFindBearings.Infrastructure.Persistence.Repositories;
using OpenFindBearings.Infrastructure.Services;
using System.Net;

namespace OpenFindBearings.Api.Extensions
{
    /// <summary>
    /// 服务注册扩展方法
    /// </summary>
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 添加HttpContext访问器
            services.AddHttpContextAccessor();

            // 添加自定义服务
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IPermissionService, PermissionService>();
            // ============ 认证服务客户端 ============
            //// 从配置获取认证服务地址
            //var authBaseUrl = configuration["Authentication:Authority"] ?? "https://localhost:7201";
            //var webAppUrl = configuration["Authentication:WebAppUrl"] ?? "https://localhost:7201/api";

            // 注册认证服务 HTTP 客户端
            services.AddHttpClient<IIdentityService, IdentityService>(client =>
            {
                client.BaseAddress = new Uri(configuration["Authentication:Authority"] ?? "https://localhost:7201");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // ============ 邀请仓储 ============
            services.AddScoped<IStaffInvitationRepository, StaffInvitationRepository>();

            // 添加响应压缩
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
            });

            return services;
        }

        public static IServiceCollection AddCorsService(this IServiceCollection services, IConfiguration configuration)
        {
            // 添加CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigins", policy =>
                {
                    var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>()
                        ?? ["http://localhost:3000", "https://localhost:7000"];

                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            return services;
        }

        public static IServiceCollection AddHealthChecksService(this IServiceCollection services, IConfiguration configuration)
        { 
            // 添加健康检查
            services.AddHealthChecks()
                 // 1. 数据库检查（必须）
                 .AddDbContextCheck<ApplicationDbContext>(
                     name: "database",
                     tags: ["db"])

                 // 2. 内存检查（可选）
                 .AddCheck<MemoryHealthCheck>(
                    name: "memory",
                    failureStatus: HealthStatus.Degraded)  // Degraded 表示降级，不是完全不可用

                // 3. 磁盘空间检查（可选）
                .AddCheck<DiskSpaceHealthCheck>(
                    name: "disk",
                    failureStatus: HealthStatus.Degraded)

                 // 4. 外部依赖检查（可选）
                 .AddUrlGroup(
                     new Uri($"{configuration["Authentication:Authority"] ?? "https://localhost:7201"}/health"),
                     name: "external_api",
                     failureStatus: HealthStatus.Degraded);

            return services;
        }

        public static IServiceCollection AddSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();

            // 只使用 Swashbuckle 的 Swagger UI
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "OpenFindBearings API",
                    Version = "v1",
                    Description = "轴承信息平台API",
                    Contact = new OpenApiContact
                    {
                        Name = "OpenFindBearings",
                        Email = "support@openfindbearings.com"
                    }
                });

                // 添加JWT认证定义
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "请输入JWT令牌，格式：Bearer {token}",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                // 添加安全需求 - 使用标准方式
                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            });

            return services;
        }

        public static IServiceCollection AddAuthenticationAndAuthorization(this IServiceCollection services, IConfiguration configuration)
        {
            // JWT 认证配置
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["Authentication:Authority"];
                options.Audience = configuration["Authentication:Audience"];
                options.RequireHttpsMetadata = configuration.GetValue<bool>("Authentication:RequireHttpsMetadata", false);

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    // 不能设置为 null，使用默认值或设置为空字符串
                    // 角色和权限在业务系统中管理，所以不验证角色
                    RoleClaimType = "role", // 使用默认值，但不会在业务中使用
                    NameClaimType = "name"
                };

                // 处理认证事件
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogError(context.Exception, "认证失败");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        // 可以在这里添加额外的 token 验证逻辑
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogDebug("Token验证成功");
                        return Task.CompletedTask;
                    }
                };
            });

            // 授权策略
            services.AddAuthorization(options =>
            {
                // 管理员策略 - 只检查是否认证，实际权限由 PermissionService 处理
                options.AddPolicy("Admin", policy =>
                    policy.RequireAuthenticatedUser());

                // 商家策略
                options.AddPolicy("Merchant", policy =>
                    policy.RequireAuthenticatedUser());

                // 同步客户端策略
                options.AddPolicy("SyncClient", policy =>
                    policy.RequireClaim("scope", "api:sync"));

                // 登录用户策略
                options.AddPolicy("Authenticated", policy =>
                    policy.RequireAuthenticatedUser());
            });

            return services;
        }

        public static IServiceCollection ConfigureForwardedHeaders(this IServiceCollection services, bool isDevelopment)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                // 所有环境都可以先打开
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto;

                // 但只在非开发环境信任网络
                if (!isDevelopment)
                {
                    AddKnownNetwork(options, "POD_NETWORK_CIDR");
                    AddKnownNetwork(options, "SERVICE_NETWORK_CIDR");
                }
                else
                {
                    // 开发环境：不信任任何代理
                    options.KnownProxies.Clear();
                    options.KnownIPNetworks.Clear();
                }
            });

            return services;
        }

        private static void AddKnownNetwork(ForwardedHeadersOptions options, string envVarName)
        {
            var cidr = Environment.GetEnvironmentVariable(envVarName);
            if (string.IsNullOrEmpty(cidr))
                return;

            try
            {
                var parts = cidr.Split('/');
                if (parts.Length == 2 &&
                    IPAddress.TryParse(parts[0], out var ip) &&
                    int.TryParse(parts[1], out var prefix))
                {
                    options.KnownIPNetworks.Add(new System.Net.IPNetwork(ip, prefix));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse CIDR ({envVarName}): {ex.Message}");
            }
        }
    }

    // ============ 自定义健康检查类 ============
    /// <summary>
    /// 内存健康检查
    /// </summary>
    public class MemoryHealthCheck : IHealthCheck
    {
        private readonly long _thresholdBytes = 512 * 1024 * 1024; // 512MB

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var usedMemory = GC.GetTotalMemory(false);

            if (usedMemory > _thresholdBytes)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Memory usage is high: {usedMemory / 1024 / 1024}MB / {_thresholdBytes / 1024 / 1024}MB"));
            }

            return Task.FromResult(HealthCheckResult.Healthy());
        }
    }

    /// <summary>
    /// 磁盘空间健康检查（可选）
    /// </summary>
    public class DiskSpaceHealthCheck : IHealthCheck
    {
        private readonly string _path;
        private readonly long _thresholdBytes; // 最小可用空间

        public DiskSpaceHealthCheck(string path = "/", long thresholdBytes = 1024 * 1024 * 1024) // 默认 1GB
        {
            _path = path;
            _thresholdBytes = thresholdBytes;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var driveInfo = new DriveInfo(_path);
                var freeSpace = driveInfo.AvailableFreeSpace;

                if (freeSpace < _thresholdBytes)
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"Low disk space: {freeSpace / 1024 / 1024}MB free, threshold: {_thresholdBytes / 1024 / 1024}MB"));
                }

                return Task.FromResult(HealthCheckResult.Healthy());
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Disk space check failed", ex));
            }
        }
    }
}