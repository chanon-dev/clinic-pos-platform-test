using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Application.Common.Models;
using ClinicPOS.Domain.Entities;

namespace ClinicPOS.Application.Users.Queries;

public class AuthenticateUserHandler
{
    private readonly IUserRepository _users;

    public AuthenticateUserHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task<Result<User>> HandleAsync(AuthenticateUserQuery query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query.Username))
            return Result<User>.Failure("Username is required.", 400);
        if (string.IsNullOrWhiteSpace(query.Password))
            return Result<User>.Failure("Password is required.", 400);

        var user = await _users.GetByUsernameAsync(query.Username, ct);
        if (user == null || !BCrypt.Net.BCrypt.Verify(query.Password, user.PasswordHash))
            return Result<User>.Failure("Invalid username or password.", 401);

        return Result<User>.Success(user);
    }
}
