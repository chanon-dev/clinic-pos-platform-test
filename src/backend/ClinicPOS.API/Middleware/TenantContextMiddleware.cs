using ClinicPOS.Application.Common.Interfaces;

namespace ClinicPOS.API.Middleware;

public class TenantContext : ITenantContext
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
}

public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("tenantId")?.Value;
            var subClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value;
            var roleClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (Guid.TryParse(tenantClaim, out var tenantId))
                tenantContext.TenantId = tenantId;
            if (Guid.TryParse(subClaim, out var userId))
                tenantContext.UserId = userId;
            tenantContext.Role = roleClaim ?? string.Empty;
        }

        await _next(context);
    }
}
