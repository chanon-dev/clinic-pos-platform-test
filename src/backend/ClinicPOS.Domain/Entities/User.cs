namespace ClinicPOS.Domain.Entities;

using ClinicPOS.Domain.Enums;

public class User
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<UserBranch> UserBranches { get; set; } = new List<UserBranch>();
}
