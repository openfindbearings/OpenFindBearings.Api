using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenFindBearings.Api.Services;
using OpenFindBearings.Application.Common.Interfaces;
using OpenFindBearings.Domain.Interfaces;
using OpenFindBearings.Infrastructure.Persistence.Repositories;
using OpenFindBearings.Infrastructure.Services;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace OpenFindBearings.Api.Extensions
{
    /// <summary>
    /// 服务注册扩展方法
    /// </summary>
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApiServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 添加HttpContext访问器
            services.AddHttpContextAccessor();

            // 添加自定义服务
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IPermissionService, PermissionService>();
            // ============ 认证服务客户端 ============
            // 从配置获取认证服务地址
            var authBaseUrl = configuration["Authentication:Authority"] ?? "https://localhost:5001";
            var webAppUrl = configuration["Authentication:WebAppUrl"] ?? "https://localhost:7000";

            // 注册认证服务 HTTP 客户端
            services.AddHttpClient<IIdentityService, IdentityService>(client =>
            {
                client.BaseAddress = new Uri(authBaseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // 配置认证服务选项（供 IdentityService 使用）
            services.Configure<IdentityServiceOptions>(options =>
            {
                options.BaseUrl = authBaseUrl;
                options.WebAppUrl = webAppUrl;
            });

            // ============ 邀请仓储 ============
            services.AddScoped<IStaffInvitationRepository, StaffInvitationRepository>();

            // 添加CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigins", policy =>
                {
                    var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>()
                        ?? new[] { "http://localhost:3000", "https://localhost:7000" };

                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            // 添加健康检查
            services.AddHealthChecks();

            // 添加响应压缩
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
            });

            return services;
        }

        public static IServiceCollection AddSwagger(
            this IServiceCollection services)
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

        public static IServiceCollection AddAuthenticationAndAuthorization(
            this IServiceCollection services,
            IConfiguration configuration)
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
                options.RequireHttpsMetadata = false;

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

                // 同步客户端策略 - 基于 client_id
                options.AddPolicy("SyncClient", policy =>
                    policy.RequireAssertion(context =>
                    {
                        var clientId = context.User.FindFirst("client_id")?.Value;
                        return clientId == "sync-client" ||
                               context.User.HasClaim(c => c.Type == "role" && c.Value == "SyncClient");
                    }));

                // 登录用户策略
                options.AddPolicy("Authenticated", policy =>
                    policy.RequireAuthenticatedUser());
            });

            return services;
        }
    }

    /// <summary>
    /// 测试认证处理器（用于开发阶段）
    /// </summary>
    public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // 开发环境返回测试用户
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "auth-admin-001"),
                new Claim(ClaimTypes.Name, "测试管理员"),
                new Claim("role", "GlobalAdmin"),
                new Claim("user_type", "Admin")
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
