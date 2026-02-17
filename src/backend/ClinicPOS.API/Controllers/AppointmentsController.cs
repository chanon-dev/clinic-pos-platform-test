using ClinicPOS.API.Middleware;
using ClinicPOS.Application.Appointments.Commands;
using ClinicPOS.Application.Appointments.Queries;
using ClinicPOS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicPOS.API.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly CreateAppointmentHandler _createHandler;
    private readonly ListAppointmentsHandler _listHandler;

    public AppointmentsController(CreateAppointmentHandler createHandler, ListAppointmentsHandler listHandler)
    {
        _createHandler = createHandler;
        _listHandler = listHandler;
    }

    [HttpPost]
    [RequirePermission(Permission.CreateAppointment)]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request)
    {
        var result = await _createHandler.HandleAsync(new CreateAppointmentCommand(
            request.PatientId, request.BranchId, request.StartAt));

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode ?? 400, new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title = result.StatusCode == 409 ? "Conflict" : "Validation Failed",
                status = result.StatusCode ?? 400,
                detail = result.Error
            });

        var a = result.Value!;
        return StatusCode(201, new
        {
            a.Id,
            a.TenantId,
            patient = a.Patient != null ? new { a.Patient.Id, a.Patient.FirstName, a.Patient.LastName } : null,
            branch = a.Branch != null ? new { a.Branch.Id, a.Branch.Name } : null,
            a.StartAt,
            a.CreatedAt
        });
    }

    [HttpGet]
    [RequirePermission(Permission.ViewAppointment)]
    public async Task<IActionResult> List([FromQuery] Guid? branchId, [FromQuery] DateOnly? date)
    {
        var appointments = await _listHandler.HandleAsync(new ListAppointmentsQuery(branchId, date));
        return Ok(new
        {
            data = appointments.Select(a => new
            {
                a.Id,
                patient = a.Patient != null ? new { a.Patient.Id, a.Patient.FirstName, a.Patient.LastName, a.Patient.PhoneNumber } : null,
                branch = a.Branch != null ? new { a.Branch.Id, a.Branch.Name } : null,
                a.StartAt,
                a.CreatedAt
            }),
            total = appointments.Count
        });
    }
}

public record CreateAppointmentRequest(Guid PatientId, Guid BranchId, DateTime StartAt);
