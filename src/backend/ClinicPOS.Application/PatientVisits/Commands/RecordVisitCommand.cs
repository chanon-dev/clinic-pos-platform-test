namespace ClinicPOS.Application.PatientVisits.Commands;

public record RecordVisitCommand(Guid PatientId, Guid BranchId, DateTime VisitedAt, string? Notes);
