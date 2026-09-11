using System.Diagnostics;
using System.Text;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Repositories;
using OpenFindBearings.Infrastructure.Persistence.Data;

namespace OpenFindBearings.Api.Middleware;

public class AuditLogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLogMiddleware> _logger;

    public AuditLogMiddleware(RequestDelegate next, ILogger<AuditLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditLogRepository repository, ApplicationDbContext dbContext)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "";

        if (ShouldSkip(method, path))
        {
            await _next(context);
            return;
        }

        string? requestBody = null;
        if (context.Request.ContentType != null &&
            context.Request.ContentType.Contains("application/json"))
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            if (!string.IsNullOrEmpty(requestBody) && requestBody.Length > 2000)
                requestBody = requestBody[..2000] + "...(truncated)";
        }

        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            try
            {
                // 改动说明：OperatorId 原取 JWT sub（Identity AuthUserId），但 FK_AuditLogs_Users
                // 指向业务库 Users.Id——两 ID 仅种子 admin 恰好一致，新注册用户必炸 23503
                // （收藏等写操作被审计写入拖崩）。改取 UserContextMiddleware 已映射的业务 UserId；
                // 无映射（客户端凭证/游客写操作）时置 null（列可空，审计不阻断）
                Guid? operatorId = context.Items["UserId"] as Guid?;
                var userName = context.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                               ?? context.User.FindFirst("preferred_username")?.Value;

                var action = MapAction(method, path);

                var entityType = ExtractEntityType(path);
                var entityId = ExtractEntityId(path);
                var statusCode = context.Response.StatusCode;

                var log = new AuditLog(
                    action,
                    entityType ?? "Unknown",
                    entityId ?? Guid.Empty,
                    operatorId,
                    remarks: requestBody,
                    httpMethod: method,
                    requestPath: path,
                    statusCode: statusCode,
                    durationMs: sw.ElapsedMilliseconds);

                await repository.AddAsync(log, context.RequestAborted);
                await dbContext.SaveChangesAsync(context.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "写入审计日志失败: {Method} {Path}", method, path);
            }
        }
    }

    private static bool ShouldSkip(string method, string path)
    {
        if (method == "GET" || method == "HEAD" || method == "OPTIONS")
            return true;
        if (path.Contains("/health", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.Contains("/swagger", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    private static string? ExtractEntityType(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < segments.Length; i++)
            if (segments[i].Equals("api", StringComparison.OrdinalIgnoreCase) && i + 1 < segments.Length)
                return segments[i + 1];
        return null;
    }

    private static Guid? ExtractEntityId(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var s in segments)
            if (Guid.TryParse(s, out var id))
                return id;
        return null;
    }

    private static string MapAction(string method, string path)
    {
        if (path.Contains("/merchants/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/verify", StringComparison.OrdinalIgnoreCase)) return "VerifyMerchant";
        if (path.Contains("/merchants/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/reject", StringComparison.OrdinalIgnoreCase)) return "RejectMerchant";
        if (path.Contains("/corrections/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/approve", StringComparison.OrdinalIgnoreCase)) return "ApproveCorrection";
        if (path.Contains("/corrections/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/reject", StringComparison.OrdinalIgnoreCase)) return "RejectCorrection";
        if (path.Contains("/licenses/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/approve", StringComparison.OrdinalIgnoreCase)) return "ApproveLicense";
        if (path.Contains("/licenses/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/reject", StringComparison.OrdinalIgnoreCase)) return "RejectLicense";
        return method switch
        {
            "POST" => "Create",
            "PUT" or "PATCH" => "Update",
            "DELETE" => "Delete",
            _ => method
        };
    }
}
