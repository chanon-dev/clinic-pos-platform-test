namespace ClinicPOS.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public int StatusCode { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
}
