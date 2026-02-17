namespace ClinicPOS.Application.Patients.Queries;

public record ListPatientsQuery(Guid? BranchId = null, string? Cursor = null, int Limit = 20, string? Search = null);
