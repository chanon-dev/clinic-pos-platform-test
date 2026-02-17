using ClinicPOS.API.Middleware;
using ClinicPOS.Application.PatientVisits.Commands;
using ClinicPOS.Application.PatientVisits.Queries;
using ClinicPOS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicPOS.API.Controllers;

[ApiController]
[Route("api/patients/{patientId:guid}/visits")]
[Authorize]
public class PatientVisitsController : ControllerBase
{
    private readonly RecordVisitHandler _recordHandler;
    private readonly GetVisitHistoryHandler _historyHandler;

    public PatientVisitsController(RecordVisitHandler recordHandler, GetVisitHistoryHandler historyHandler)
    {
        _recordHandler = recordHandler;
        _historyHandler = historyHandler;
    }

    [HttpPost]
    [RequirePermission(Permission.CreatePatient)]
    public async Task<IActionResult> RecordVisit(Guid patientId, [FromBody] RecordVisitRequest request)
    {
        var result = await _recordHandler.HandleAsync(new RecordVisitCommand(
            patientId, request.BranchId, request.VisitedAt, request.Notes));

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode ?? 400, new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title = result.StatusCode == 404 ? "Not Found" : "Validation Failed",
                status = result.StatusCode ?? 400,
                detail = result.Error
            });

        var v = result.Value!;
        return StatusCode(201, new
        {
            v.Id,
            v.PatientId,
            v.BranchId,
            branch = v.Branch != null ? new { v.Branch.Id, v.Branch.Name } : null,
            v.VisitedAt,
            v.Notes,
            v.CreatedAt
        });
    }

    [HttpGet]
    [RequirePermission(Permission.ViewPatient)]
    public async Task<IActionResult> GetHistory(Guid patientId)
    {
        var visits = await _historyHandler.HandleAsync(new GetVisitHistoryQuery(patientId));
        return Ok(new
        {
            data = visits.Select(v => new
            {
                v.Id,
                v.PatientId,
                v.BranchId,
                branch = v.Branch != null ? new { v.Branch.Id, v.Branch.Name } : null,
                v.VisitedAt,
                v.Notes,
                v.CreatedAt
            })
        });
    }
}

public record RecordVisitRequest(Guid BranchId, DateTime VisitedAt, string? Notes);
