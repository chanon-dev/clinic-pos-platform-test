using ClinicPOS.API.Middleware;
using ClinicPOS.Application.Users.Commands;
using ClinicPOS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicPOS.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly CreateUserHandler _createHandler;
    private readonly AssignRoleHandler _assignRoleHandler;
    private readonly AssociateUserBranchesHandler _branchHandler;

    public UsersController(
        CreateUserHandler createHandler,
        AssignRoleHandler assignRoleHandler,
        AssociateUserBranchesHandler branchHandler)
    {
        _createHandler = createHandler;
        _assignRoleHandler = assignRoleHandler;
        _branchHandler = branchHandler;
    }

    [HttpPost]
    [RequirePermission(Permission.ManageUsers)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var result = await _createHandler.HandleAsync(new CreateUserCommand(
            request.Username, request.Password, request.Role, request.BranchIds));

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode ?? 400, new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title = result.StatusCode == 409 ? "Conflict" : "Validation Failed",
                status = result.StatusCode ?? 400,
                detail = result.Error
            });

        var u = result.Value!;
        return StatusCode(201, new
        {
            u.Id, u.Username, role = u.Role.ToString(), u.TenantId,
            branches = u.UserBranches.Select(ub => new { ub.BranchId }),
            u.CreatedAt
        });
    }

    [HttpPut("{userId}/role")]
    [RequirePermission(Permission.ManageUsers)]
    public async Task<IActionResult> AssignRole(Guid userId, [FromBody] AssignRoleRequest request)
    {
        var result = await _assignRoleHandler.HandleAsync(new AssignRoleCommand(userId, request.Role));

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode ?? 400, new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title = "Error",
                status = result.StatusCode,
                detail = result.Error
            });

        var u = result.Value!;
        return Ok(new { u.Id, u.Username, role = u.Role.ToString(), u.TenantId });
    }

    [HttpPut("{userId}/branches")]
    [RequirePermission(Permission.ManageUsers)]
    public async Task<IActionResult> AssociateBranches(Guid userId, [FromBody] AssociateBranchesRequest request)
    {
        var result = await _branchHandler.HandleAsync(
            new AssociateUserBranchesCommand(userId, request.BranchIds));

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode ?? 400, new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title = "Error",
                status = result.StatusCode,
                detail = result.Error
            });

        var u = result.Value!;
        return Ok(new
        {
            u.Id, u.Username,
            branches = u.UserBranches.Select(ub => new { ub.Branch.Id, ub.Branch.Name })
        });
    }
}

public record CreateUserRequest(string Username, string Password, string Role, List<Guid>? BranchIds);
public record AssignRoleRequest(string Role);
public record AssociateBranchesRequest(List<Guid> BranchIds);
