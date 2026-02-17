using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Application.Common.Models;
using ClinicPOS.Domain.Entities;
using ClinicPOS.Domain.Enums;

namespace ClinicPOS.Application.Users.Commands;

public class CreateUserHandler
{
    private readonly IUserRepository _users;
    private readonly ITenantContext _tenantContext;

    public CreateUserHandler(IUserRepository users, ITenantContext tenantContext)
    {
        _users = users;
        _tenantContext = tenantContext;
    }

    public async Task<Result<User>> HandleAsync(CreateUserCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Username) || command.Username.Length < 3)
            return Result<User>.Failure("Username must be at least 3 characters.");
        if (string.IsNullOrWhiteSpace(command.Password) || command.Password.Length < 6)
            return Result<User>.Failure("Password must be at least 6 characters.");
        if (!Enum.TryParse<Role>(command.Role, true, out var role))
            return Result<User>.Failure("Invalid role. Must be Admin, User, or Viewer.");

        var existing = await _users.GetByUsernameAsync(command.Username, ct);
        if (existing != null)
            return Result<User>.Failure("Username already exists.", 409);

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Username = command.Username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.Password),
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        if (command.BranchIds?.Any() == true)
        {
            user.UserBranches = command.BranchIds
                .Select(bid => new UserBranch { UserId = user.Id, BranchId = bid })
                .ToList();
        }

        await _users.AddAsync(user, ct);
        return Result<User>.Success(user);
    }
}
