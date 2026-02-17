namespace ClinicPOS.Domain.Entities;

public class PatientVisit
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PatientId { get; set; }
    public Guid BranchId { get; set; }
    public DateTime VisitedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public Patient? Patient { get; set; }
    public Branch? Branch { get; set; }
}
