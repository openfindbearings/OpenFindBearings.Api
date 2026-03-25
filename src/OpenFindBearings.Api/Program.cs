using OpenFindBearings.Api.Extensions;
using OpenFindBearings.Api.Middleware;
using OpenFindBearings.Application;
using OpenFindBearings.Infrastructure;
using OpenFindBearings.Infrastructure.Persistence.Data;

var builder = WebApplication.CreateBuilder(args);

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
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
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
        await context.Response.WriteAsJsonAsync(response);
    }
});

// 映射所有 API 端点
app.MapApiEndpoints();

// ============ 初始化数据库 ============
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        // 确保数据库创建
        await context.Database.EnsureCreatedAsync();

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
