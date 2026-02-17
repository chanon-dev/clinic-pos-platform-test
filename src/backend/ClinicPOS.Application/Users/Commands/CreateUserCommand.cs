namespace ClinicPOS.Application.Users.Commands;

public record CreateUserCommand(
    string Username,
    string Password,
    string Role,
    List<Guid>? BranchIds);
