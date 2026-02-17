using ClinicPOS.API.Middleware;
using ClinicPOS.Application.Common.Interfaces;
using ClinicPOS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicPOS.API.Controllers;

[ApiController]
[Route("api/branches")]
[Authorize]
public class BranchesController : ControllerBase
{
    private readonly IBranchRepository _branches;

    public BranchesController(IBranchRepository branches)
    {
        _branches = branches;
    }

    [HttpGet]
    [RequirePermission(Permission.ViewPatient)]
    public async Task<IActionResult> List()
    {
        var branches = await _branches.GetAllAsync();
        return Ok(new
        {
            data = branches.Select(b => new { b.Id, b.Name, b.CreatedAt })
        });
    }
}
