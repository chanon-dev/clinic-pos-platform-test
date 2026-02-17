using ClinicPOS.API.Middleware;
using ClinicPOS.Application.Patients.Commands;
using ClinicPOS.Application.Patients.Queries;
using ClinicPOS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicPOS.API.Controllers;

[ApiController]
[Route("api/patients")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly CreatePatientHandler _createHandler;
    private readonly ListPatientsHandler _listHandler;

    public PatientsController(CreatePatientHandler createHandler, ListPatientsHandler listHandler)
    {
        _createHandler = createHandler;
        _listHandler = listHandler;
    }

    [HttpPost]
    [RequirePermission(Permission.CreatePatient)]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request)
    {
        var result = await _createHandler.HandleAsync(new CreatePatientCommand(
            request.FirstName, request.LastName, request.PhoneNumber, request.PrimaryBranchId));

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode ?? 400, new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title = result.StatusCode == 409 ? "Conflict" : "Validation Failed",
                status = result.StatusCode ?? 400,
                detail = result.Error
            });

        var p = result.Value!;
        return StatusCode(201, new
        {
            p.Id,
            p.FirstName,
            p.LastName,
            p.PhoneNumber,
            p.TenantId,
            primaryBranch = p.PrimaryBranch != null ? new { p.PrimaryBranch.Id, p.PrimaryBranch.Name } : null,
            p.CreatedAt
        });
    }

    [HttpGet]
    [RequirePermission(Permission.ViewPatient)]
    public async Task<IActionResult> List([FromQuery] Guid? branchId, [FromQuery] string? cursor, [FromQuery] int limit = 20, [FromQuery] string? search = null)
    {
        var result = await _listHandler.HandleAsync(new ListPatientsQuery(branchId, cursor, limit, search));
        return Ok(new
        {
            data = result.Items.Select(p => new
            {
                p.Id,
                p.FirstName,
                p.LastName,
                p.PhoneNumber,
                primaryBranch = p.PrimaryBranch != null ? new { p.PrimaryBranch.Id, p.PrimaryBranch.Name } : null,
                p.CreatedAt
            }),
            total = result.TotalCount,
            nextCursor = result.NextCursor,
            hasMore = result.HasMore
        });
    }
}

public record CreatePatientRequest(string FirstName, string LastName, string PhoneNumber, Guid? PrimaryBranchId);
