namespace ClinicPOS.Application.Appointments.Commands;

public record CreateAppointmentCommand(Guid PatientId, Guid BranchId, DateTime StartAt);
