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

// 添加 Swagger/OpenAPI
builder.Services.AddSwagger();

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
