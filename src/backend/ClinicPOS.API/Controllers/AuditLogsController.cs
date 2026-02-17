using ClinicPOS.API.Middleware;
using ClinicPOS.Domain.Enums;
using ClinicPOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicPOS.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenantContext;

    public AuditLogsController(AppDbContext db, TenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    [RequirePermission(Permission.ManageUsers)]
    public async Task<IActionResult> List([FromQuery] string? entityType, [FromQuery] int limit = 50)
    {
        limit = Math.Clamp(limit, 1, 200);

        var query = _db.AuditLogs
            .Where(a => a.TenantId == _tenantContext.TenantId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .Select(a => new
            {
                a.Id,
                a.UserId,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.StatusCode,
                a.Details,
                a.Timestamp
            })
            .ToListAsync();

        return Ok(new { data = logs });
    }
}
