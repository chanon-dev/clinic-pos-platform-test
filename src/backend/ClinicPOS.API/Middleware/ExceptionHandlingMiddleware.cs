using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ClinicPOS.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate key") == true
            || ex.InnerException?.Message.Contains("unique constraint") == true)
        {
            context.Response.StatusCode = 409;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title = "Conflict",
                status = 409,
                detail = "A record with the same unique constraint already exists."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title = "Internal Server Error",
                status = 500,
                detail = "An unexpected error occurred."
            });
        }
    }
}
