using Microsoft.EntityFrameworkCore;
using OpenFindBearings.Api.Extensions;
using OpenFindBearings.Api.Middleware;
using OpenFindBearings.Application;
using OpenFindBearings.Infrastructure;
using OpenFindBearings.Infrastructure.Persistence.Data;

var builder = WebApplication.CreateBuilder(args);

// 配置转发头
builder.Services.ConfigureForwardedHeaders(builder.Environment.IsDevelopment());

// 添加各层服务
builder.Services.AddApplication();                         // Application 层 (MediatR + FluentValidation)
builder.Services.AddInfrastructure(builder.Configuration); // Infrastructure 层 (EF Core + Repositories)
builder.Services.AddApiServices(builder.Configuration);    // API 层服务

// 添加跨域
builder.Services.AddCorsService(builder.Configuration);

// 添加认证和授权
builder.Services.AddAuthenticationAndAuthorization(builder.Configuration);

// 添加 OpenAPI
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi();
}

// 添加健康检查
builder.Services.AddHealthChecksService(builder.Configuration);

// ============ 构建应用 ============
var app = builder.Build();
app.Logger.LogInformation("启动 OpenFindBearings API");

// 转发头
app.UseForwardedHeaders();

// 开发环境特定配置
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
}

// ============ 中间件顺序（重要：从上到下执行）============

// 1. 全局异常处理（必须最前面，捕获所有异常）
app.UseMiddleware<ExceptionHandlingMiddleware>();
// 2. 限流中间件（在请求进入业务逻辑前拦截）
app.UseMiddleware<RateLimitingMiddleware>();
// 3. 日志中间件（请求日志和用户行为收集）
app.UseMiddleware<ApiLoggingMiddleware>();


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

app.UseStaticFiles();

// 映射所有 API 端点
app.MapApiEndpoints();

// 健康检查
app.MapAllMapHealthChecks();

// ==========================================
// 执行数据库初始化
// TODO: 支持--init参数通过InitContainer实现单独的初始化工作
// ==========================================
await InitializeDatabaseAsync(app);

// ============ 启动应用 ============
app.Run();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();

    try
    {
        // 使用迁移
        await context.Database.MigrateAsync();
        // 填充数据
        await SeedData.SeedAsync(context, app.Environment.IsDevelopment());

        app.Logger.LogInformation("数据库初始化成功");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "数据库初始化失败");
    }
}
