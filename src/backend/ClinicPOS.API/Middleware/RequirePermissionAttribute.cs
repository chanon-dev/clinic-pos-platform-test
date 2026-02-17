using ClinicPOS.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClinicPOS.API.Middleware;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly Permission _permission;

    public RequirePermissionAttribute(Permission permission)
    {
        _permission = permission;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var roleClaim = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(roleClaim) || !Enum.TryParse<Role>(roleClaim, out var role))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (!HasPermission(role, _permission))
        {
            context.Result = new ObjectResult(new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title = "Forbidden",
                status = 403,
                detail = "You do not have permission to perform this action."
            })
            { StatusCode = 403 };
        }
    }

    private static bool HasPermission(Role role, Permission permission)
    {
        return role switch
        {
            Role.Admin => true,
            Role.User => permission is Permission.ViewPatient
                or Permission.CreatePatient
                or Permission.ViewAppointment
                or Permission.CreateAppointment,
            Role.Viewer => permission is Permission.ViewPatient
                or Permission.ViewAppointment,
            _ => false
        };
    }
}
