using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenFindBearings.Api.Endpoints;

namespace OpenFindBearings.Api.Extensions
{
    /// <summary>
    /// 端点映射扩展方法
    /// </summary>
    public static class EndpointRouteBuilderExtensions
    {
        public static void MapApiEndpoints(this IEndpointRouteBuilder app)
        {
            // ============ 按模块映射端点 ============

            // 公共接口（无需认证）
            app.MapPublicEndpoints();

            // 移动端接口（部分需要认证）
            app.MapMobileEndpoints();

            // 用户接口（需登录）
            app.MapUserEndpoints();

            // 商家管理接口（需商家角色）
            app.MapMerchantEndpoints();

            // 管理员接口（需管理员角色）
            app.MapAdminEndpoints();

            // 同步接口（限 sync_client）
            app.MapSyncEndpoints();
        }

        public static void MapAllMapHealthChecks(this IEndpointRouteBuilder app)
        {
            // 传统风格（友好响应）
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
                    // 修改这里：只有 Unhealthy 才返回 503，Degraded 返回 200
                    var statusCode = report.Status == HealthStatus.Unhealthy ? 503 : 200;
                    context.Response.StatusCode = statusCode;

                    await context.Response.WriteAsync(report.Status.ToString());
                }
            });

            // --- A. 存活探针 (/live) ---
            // 职责：只检查进程是否死锁。
            // 策略：不执行任何注册的检查项 (Predicate = false)。
            app.MapHealthChecks("/live", new HealthCheckOptions
            {
                Predicate = _ => false
            });

            // --- B. 就绪探针 (/ready) ---
            // 职责：检查是否准备好接收流量。
            // 【修复点】：排除 "db" 标签的检查。
            // 原因：在数据库迁移期间，数据库连接可能被占用。如果这里检查数据库，会导致就绪探针失败，
            // 进而导致 K8s 认为服务未就绪甚至重启服务，导致迁移永远无法完成。
            app.MapHealthChecks("/ready", new HealthCheckOptions
            {
                Predicate = check => !check.Tags.Contains("db")
            });
        }
    }
}
