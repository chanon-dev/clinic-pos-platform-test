namespace ClinicPOS.Application.Appointments.Queries;

public record ListAppointmentsQuery(Guid? BranchId = null, DateOnly? Date = null);
