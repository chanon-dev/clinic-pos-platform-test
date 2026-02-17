using ClinicPOS.Application.Users.Queries;
using ClinicPOS.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicPOS.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthenticateUserHandler _handler;
    private readonly JwtTokenService _jwt;

    public AuthController(AuthenticateUserHandler handler, JwtTokenService jwt)
    {
        _handler = handler;
        _jwt = jwt;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _handler.HandleAsync(
            new AuthenticateUserQuery(request.Username, request.Password));

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode ?? 401, new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title = "Unauthorized",
                status = result.StatusCode ?? 401,
                detail = result.Error
            });

        var user = result.Value!;
        var (token, expiresAt) = _jwt.GenerateToken(user);

        return Ok(new
        {
            token,
            expiresAt,
            user = new
            {
                user.Id,
                user.Username,
                role = user.Role.ToString(),
                user.TenantId,
                branches = user.UserBranches.Select(ub => new
                {
                    ub.Branch.Id,
                    ub.Branch.Name
                })
            }
        });
    }
}

public record LoginRequest(string Username, string Password);
