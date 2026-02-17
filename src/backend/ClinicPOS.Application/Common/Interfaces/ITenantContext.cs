namespace ClinicPOS.Application.Common.Interfaces;

public interface ITenantContext
{
    Guid TenantId { get; }
    Guid UserId { get; }
    string Role { get; }
}
