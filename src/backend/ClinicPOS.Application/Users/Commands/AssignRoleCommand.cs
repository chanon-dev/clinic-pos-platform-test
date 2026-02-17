namespace ClinicPOS.Application.Users.Commands;

public record AssignRoleCommand(Guid UserId, string Role);
