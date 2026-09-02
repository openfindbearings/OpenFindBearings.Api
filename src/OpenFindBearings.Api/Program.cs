using OpenFindBearings.Api;
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

// 改动说明：DateTime 统一输出为 UTC ISO 8601 带 Z 后缀，确保前端时区转换正确
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new UtcDateTimeConverter());
    options.SerializerOptions.Converters.Add(new NullableUtcDateTimeConverter());
});

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

// HTTPS 重定向
app.UseHttpsRedirection();

// CORS
app.UseCors("AllowSpecificOrigins");

// 认证和授权
app.UseAuthentication();      // 必须在这
app.UseMiddleware<UserContextMiddleware>(); // 在认证之后，授权之前

// 2. 日志中间件（请求日志和用户行为）
// 改动说明：原实现注册在认证与用户上下文之前，导致其读取的 Items["UserId"]/Items["SessionId"]
//           恒为空——这两个值只能由 UserContextMiddleware 写入。结果是 ApiCallLog 全部
//           丢失用户维度，基于用户ID的地区偏好统计也永不触发。
//           移至用户上下文之后可获得真实用户身份；同时保持在限流之前，使被限流的
//           请求（429）仍能被完整记录
app.UseMiddleware<ApiLoggingMiddleware>();

// 3. 限流中间件
// 改动说明：原实现注册在认证之前，导致 HttpContext.User 尚未填充、Items["UserId"] 也未写入，
//           限流判定恒为"未登录游客"并按出口 IP 限流，RateLimit.User / Premium 配置完全不可达。
//           移至认证与用户上下文之后，才能按真实用户身份与角色分配配额（API 官方推荐位置）。
app.UseMiddleware<RateLimitingMiddleware>();

app.UseAuthorization();       // 授权

// 响应压缩
app.UseResponseCompression();

// 4. 审计日志记录（在授权之后，确保用户身份已解析）
app.UseMiddleware<AuditLogMiddleware>();

// 为了robots.txt，使用静态文件
app.UseStaticFiles();

// 映射所有 API 端点
app.MapApiEndpoints();

// 健康检查
app.MapAllMapHealthChecks();

// 执行数据库初始化
using var scope = app.Services.CreateScope();
await SeedData.SeedAsync(scope.ServiceProvider, app.Logger, app.Environment.IsDevelopment());

// 启动
app.Run();
