namespace ClinicPOS.Application.Users.Commands;

public record AssociateUserBranchesCommand(Guid UserId, List<Guid> BranchIds);
