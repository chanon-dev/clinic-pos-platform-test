using ClinicPOS.Domain.Entities;
using ClinicPOS.Infrastructure.Persistence;

namespace ClinicPOS.API.Middleware;

public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly HashSet<string> MutationMethods = ["POST", "PUT", "DELETE", "PATCH"];

    public AuditLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (!ShouldAudit(context)) return;

        try
        {
            using var scope = context.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var path = context.Request.Path.Value ?? "";
            var entityType = ExtractEntityType(path);

            Guid? tenantId = null;
            Guid? userId = null;
            var tenantClaim = context.User.FindFirst("tenantId")?.Value;
            var subClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value;

            if (Guid.TryParse(tenantClaim, out var tid)) tenantId = tid;
            if (Guid.TryParse(subClaim, out var uid)) userId = uid;

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                Action = context.Request.Method,
                EntityType = entityType,
                StatusCode = context.Response.StatusCode,
                Details = $"{context.Request.Method} {path}",
                Timestamp = DateTime.UtcNow
            };

            db.AuditLogs.Add(auditLog);
            await db.SaveChangesAsync();
        }
        catch
        {
            // Audit logging should never break the request
        }
    }

    private static bool ShouldAudit(HttpContext context)
    {
        if (!MutationMethods.Contains(context.Request.Method)) return false;

        var path = context.Request.Path.Value ?? "";
        if (!path.StartsWith("/api/")) return false;
        if (path.StartsWith("/api/auth/")) return false;

        var status = context.Response.StatusCode;
        return status >= 200 && status < 300;
    }

    private static string ExtractEntityType(string path)
    {
        // /api/patients -> Patient, /api/appointments -> Appointment, etc.
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 2)
        {
            var entity = segments[1]; // "patients", "appointments", etc.
            // Singularize and PascalCase
            if (entity.EndsWith("s")) entity = entity[..^1];
            return char.ToUpper(entity[0]) + entity[1..];
        }
        return "Unknown";
    }
}
