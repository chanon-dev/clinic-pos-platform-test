using System.Collections.Concurrent;
using System.Threading.RateLimiting;

namespace ClinicPOS.API.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, RateLimiter> Limiters = new();
    private static readonly HashSet<string> ExemptPaths = ["/health", "/api/auth/login"];
    private static readonly HashSet<string> MutationMethods = ["POST", "PUT", "DELETE", "PATCH"];

    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (ExemptPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var tenantClaim = context.User.FindFirst("tenantId")?.Value;
        if (string.IsNullOrEmpty(tenantClaim))
        {
            await _next(context);
            return;
        }

        var isMutation = MutationMethods.Contains(context.Request.Method);
        var key = $"{tenantClaim}:{(isMutation ? "write" : "read")}";

        var limiter = Limiters.GetOrAdd(key, _ => new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = isMutation ? 100 : 300,
            Window = TimeSpan.FromMinutes(1),
            AutoReplenishment = true,
            QueueLimit = 0
        }));

        using var lease = await limiter.AcquireAsync(1, context.RequestAborted);

        if (!lease.IsAcquired)
        {
            context.Response.StatusCode = 429;
            context.Response.Headers.RetryAfter = "60";
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title = "Too Many Requests",
                status = 429,
                detail = "Rate limit exceeded for your tenant. Please try again later."
            });
            return;
        }

        await _next(context);
    }
}
