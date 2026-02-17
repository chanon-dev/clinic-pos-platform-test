using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Application.Common.Models;
using ClinicPOS.Domain.Entities;

namespace ClinicPOS.Application.Appointments.Commands;

public class CreateAppointmentHandler
{
    private readonly IAppointmentRepository _appointments;
    private readonly ITenantContext _tenantContext;
    private readonly IEventPublisher _eventPublisher;

    public CreateAppointmentHandler(
        IAppointmentRepository appointments,
        ITenantContext tenantContext,
        IEventPublisher eventPublisher)
    {
        _appointments = appointments;
        _tenantContext = tenantContext;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<Appointment>> HandleAsync(CreateAppointmentCommand command, CancellationToken ct = default)
    {
        if (command.PatientId == Guid.Empty)
            return Result<Appointment>.Failure("Patient is required.");
        if (command.BranchId == Guid.Empty)
            return Result<Appointment>.Failure("Branch is required.");
        if (command.StartAt <= DateTime.UtcNow)
            return Result<Appointment>.Failure("Appointment time must be in the future.");

        // Check for duplicate (same patient, branch, time)
        if (await _appointments.ExistsAsync(command.PatientId, command.BranchId, command.StartAt, ct))
            return Result<Appointment>.Failure(
                "An appointment already exists for this patient at the same time and branch.", 409);

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            PatientId = command.PatientId,
            BranchId = command.BranchId,
            StartAt = command.StartAt,
            CreatedAt = DateTime.UtcNow
        };

        await _appointments.AddAsync(appointment, ct);

        // Publish event (fire-and-forget, don't fail the request)
        _ = _eventPublisher.PublishAsync("clinic", "appointment.created", new
        {
            appointment.Id,
            appointment.TenantId,
            appointment.PatientId,
            appointment.BranchId,
            appointment.StartAt,
            appointment.CreatedAt
        }, ct);

        return Result<Appointment>.Success(appointment);
    }
}
