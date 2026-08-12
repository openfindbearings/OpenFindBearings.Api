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

        try
        {
            await _next(context);
        }
        finally
        {
            try
            {
                var subClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                               ?? context.User.FindFirst("sub")?.Value;
                Guid? operatorId = Guid.TryParse(subClaim, out var uid) ? uid : null;
                var userName = context.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                               ?? context.User.FindFirst("preferred_username")?.Value;

                var action = method switch
                {
                    "POST" => "Create",
                    "PUT" => "Update",
                    "PATCH" => "Update",
                    "DELETE" => "Delete",
                    _ => method
                };

                var entityType = ExtractEntityType(path);
                var entityId = ExtractEntityId(path);

                var log = new AuditLog(
                    action,
                    entityType ?? "Unknown",
                    entityId ?? Guid.Empty,
                    operatorId,
                    remarks: $"{method} {path} -> {context.Response.StatusCode}" +
                             (requestBody != null ? $" Body: {requestBody}" : ""));

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
}
