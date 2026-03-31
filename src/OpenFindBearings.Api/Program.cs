using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenFindBearings.Api.Extensions;
using OpenFindBearings.Api.Middleware;
using OpenFindBearings.Application;
using OpenFindBearings.Infrastructure;
using OpenFindBearings.Infrastructure.Persistence.Data;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    if (!builder.Environment.IsDevelopment())
    {
        var podCidr = Environment.GetEnvironmentVariable("POD_NETWORK_CIDR");
        if (!string.IsNullOrEmpty(podCidr))
        {
            try
            {
                var parts = podCidr.Split('/');
                if (parts.Length == 2 &&
                    IPAddress.TryParse(parts[0], out var ip) &&
                    int.TryParse(parts[1], out var prefix))
                {
                    options.KnownIPNetworks.Add(new System.Net.IPNetwork(ip, prefix));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse CIDR: {ex.Message}");
            }
        }

        // 可选：添加 Service 网络 CIDR
        var serviceCidr = Environment.GetEnvironmentVariable("SERVICE_NETWORK_CIDR");
        if (!string.IsNullOrEmpty(serviceCidr))
        {
            var parts = serviceCidr.Split('/');
            if (parts.Length == 2 &&
                IPAddress.TryParse(parts[0], out var ip) &&
                int.TryParse(parts[1], out var prefix))
            {
                options.KnownIPNetworks.Add(new System.Net.IPNetwork(ip, prefix));
            }
        }
    }
});


// ============ 添加服务 ============

// 添加各层服务
builder.Services.AddApplication();                         // Application 层 (MediatR + FluentValidation)
builder.Services.AddInfrastructure(builder.Configuration); // Infrastructure 层 (EF Core + Repositories)
builder.Services.AddApiServices(builder.Configuration);    // API 层服务 (CORS, 健康检查等)

// 添加认证和授权
builder.Services.AddAuthenticationAndAuthorization(builder.Configuration);

// 添加 Swagger/OpenAPI
builder.Services.AddSwagger();

// ============ 构建应用 ============
var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("启动 OpenFindBearings API");

// ============ 配置中间件管道 ============

app.UseForwardedHeaders();

// 开发环境特定配置
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OpenFindBearings API V1");
        c.RoutePrefix = "swagger";
    });

    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
}

// 全局异常处理中间件（必须放在最前面）
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 请求日志中间件
app.UseMiddleware<RequestLoggingMiddleware>();

// HTTPS 重定向
app.UseHttpsRedirection();

// CORS
app.UseCors("AllowSpecificOrigins");

// 认证和授权
app.UseAuthentication();      // 必须在这
app.UseMiddleware<UserContextMiddleware>(); // 在认证之后，授权之前
app.UseAuthorization();       // 授权

// 响应压缩
app.UseResponseCompression();

// 健康检查
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            }),
            duration = report.TotalDuration
        };
        await context.Response.WriteAsJsonAsync(result);
    }
});

// K8s 风格（简洁响应）
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.StatusCode = report.Status == HealthStatus.Healthy ? 200 : 503;
        await context.Response.WriteAsync(report.Status.ToString());
    }
});

// K8s 就绪探针
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.StatusCode = report.Status == HealthStatus.Healthy ? 200 : 503;
    }
});

// K8s 存活探针（只检查进程是否存活）
app.MapHealthChecks("/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// 映射所有 API 端点
app.MapApiEndpoints();

// ============ 初始化数据库 ============
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // 确保数据库创建
        await context.Database.MigrateAsync();

        // 填充种子数据
        await SeedData.SeedAsync(context);

        logger.LogInformation("数据库初始化成功");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "数据库初始化失败");
    }
}

// ============ 启动应用 ============
app.Run();
