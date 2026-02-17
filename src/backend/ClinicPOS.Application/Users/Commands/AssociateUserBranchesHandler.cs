using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Application.Common.Models;
using ClinicPOS.Domain.Entities;

namespace ClinicPOS.Application.Users.Commands;

public class AssociateUserBranchesHandler
{
    private readonly IUserRepository _users;

    public AssociateUserBranchesHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task<Result<User>> HandleAsync(AssociateUserBranchesCommand command, CancellationToken ct = default)
    {
        if (command.BranchIds == null || !command.BranchIds.Any())
            return Result<User>.Failure("At least one branch must be specified.");

        var user = await _users.GetByIdAsync(command.UserId, ct);
        if (user == null)
            return Result<User>.Failure("User not found.", 404);

        user.UserBranches.Clear();
        foreach (var branchId in command.BranchIds)
        {
            user.UserBranches.Add(new UserBranch { UserId = user.Id, BranchId = branchId });
        }

        await _users.UpdateAsync(user, ct);
        return Result<User>.Success(user);
    }
}
