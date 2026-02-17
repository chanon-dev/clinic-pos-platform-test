using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Application.Common.Models;
using ClinicPOS.Domain.Entities;
using ClinicPOS.Domain.Enums;

namespace ClinicPOS.Application.Users.Commands;

public class AssignRoleHandler
{
    private readonly IUserRepository _users;

    public AssignRoleHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task<Result<User>> HandleAsync(AssignRoleCommand command, CancellationToken ct = default)
    {
        if (!Enum.TryParse<Role>(command.Role, true, out var role))
            return Result<User>.Failure("Invalid role. Must be Admin, User, or Viewer.");

        var user = await _users.GetByIdAsync(command.UserId, ct);
        if (user == null)
            return Result<User>.Failure("User not found.", 404);

        user.Role = role;
        await _users.UpdateAsync(user, ct);
        return Result<User>.Success(user);
    }
}
