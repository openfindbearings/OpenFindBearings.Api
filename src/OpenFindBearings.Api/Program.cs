using OpenFindBearings.Api;
using OpenFindBearings.Api.Extensions;
using OpenFindBearings.Api.Middleware;
using OpenFindBearings.Application;
using OpenFindBearings.Infrastructure;
using OpenFindBearings.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

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

// ============ 启动时自动迁移数据库 ============
// 改动说明：此前 schema 靠手工/CI 应用，生产库曾停留在 InitialCreate，导致 MerchantMembers 等
//   新表缺失、相关接口（认领搜索等）在运行时 500。改为启动即把库追平到代码内最新迁移，
//   杜绝"库落后于代码"这类部署事故。当前为单副本部署，直接顺序执行 Migrate 即可；
//   若将来扩到多副本，需在 Migrate 外加 Postgres advisory lock 或改为部署前一次性迁移 Job，
//   以避免滚动更新时新旧 Pod 并发迁移。迁移异常直接抛出使启动失败，快速暴露而非带错误 schema 服务。
using (var migrateScope = app.Services.CreateScope())
{
    var db = migrateScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (db.Database.IsRelational())
    {
        // 改动说明：GetPendingMigrationsAsync 返回 IAsyncEnumerable 不可 await，改用同步枚举以支持 Any/Count
        var pendingMigrations = db.Database.GetPendingMigrations().ToList();
        if (pendingMigrations.Count > 0)
        {
            app.Logger.LogInformation("检测到待应用迁移 {Count} 项，开始迁移数据库", pendingMigrations.Count);
            // 改动说明（v1.31.0）：迁移加 Postgres 会话级咨询锁——滚动更新 surge/手动双实例场景下
            //   防新旧 Pod 并发应用同一迁移（互相 DDL 死锁）；锁随连接关闭自动释放，Pod 被杀不留死锁。
            //   先显式打开连接再迁移：MigrateAsync 复用同一连接，锁与迁移在同一会话上生效
            var conn = db.Database.GetDbConnection();
            await conn.OpenAsync();
            try
            {
                await using (var lockCmd = conn.CreateCommand())
                {
                    lockCmd.CommandText = "SELECT pg_advisory_lock(63820515)"; // 锁键=本项目固定常量（ofb 谐音）
                    await lockCmd.ExecuteScalarAsync();
                }
                await db.Database.MigrateAsync();
                app.Logger.LogInformation("数据库迁移完成，已应用：{Migrations}", string.Join(", ", pendingMigrations));
            }
            finally
            {
                // 显式解锁后关连接（会话锁本随连接消亡，unlock 仅为语义清晰）
                await using var unlockCmd = conn.CreateCommand();
                unlockCmd.CommandText = "SELECT pg_advisory_unlock(63820515)";
                await unlockCmd.ExecuteScalarAsync();
                await conn.CloseAsync();
            }
        }
        else
        {
            app.Logger.LogDebug("数据库已是最新，无需迁移");
        }

        // v1.36.1：读业务时区偏移（SystemConfig Business.TimeZoneOffsetHours，WordPress options 式
        // 管理员定义配置；不读服务器/DB 会话时区——容器默认 UTC、连接串又钉 Timezone=UTC，取了必错）。
        // BusinessClock 日界影响签到/登录/额度幂等键，启动即定、改后重启生效（滚动期新旧偏移并存
        // 会导致部分用户当日双发）；配置缺失/读取失败保持默认 UTC+8，不阻塞启动
        try
        {
            var tzConn = db.Database.GetDbConnection();
            await tzConn.OpenAsync();
            try
            {
                await using var tzCmd = tzConn.CreateCommand();
                tzCmd.CommandText = "SELECT \"Value\" FROM \"SystemConfigs\" WHERE \"Key\" = 'Business.TimeZoneOffsetHours' LIMIT 1";
                var tzVal = await tzCmd.ExecuteScalarAsync();
                if (tzVal != null && int.TryParse(tzVal.ToString(), out var tzHours))
                {
                    OpenFindBearings.Domain.Services.BusinessClock.Configure(tzHours);
                    app.Logger.LogInformation("业务日界偏移已配置：UTC{TzHours:+0;-0;+0}", tzHours);
                }
            }
            finally
            {
                await tzConn.CloseAsync();
            }
        }
        catch (Exception tzEx)
        {
            app.Logger.LogWarning(tzEx, "业务时区配置读取失败，使用默认 UTC+8");
        }
    }
}

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
