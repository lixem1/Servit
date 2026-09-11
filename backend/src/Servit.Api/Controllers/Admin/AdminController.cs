using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Servit.Api.Contracts.Admin;
using Servit.Api.Extensions;
using Servit.Domain.Constants;
using Servit.Domain.Entities;

namespace Servit.Api.Controllers.Admin;

// Base controller for the admin web panel. Every route under /api/admin requires the
// Admin role, so future Fase 1 admin endpoints can inherit from or sit beside this one.
[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin")]
public class AdminController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<AdminMeResponse>> GetMe()
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());
        if (user is null) return NotFound();

        var roles = await userManager.GetRolesAsync(user);
        return Ok(new AdminMeResponse
        {
            Id = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            Roles = roles.ToList()
        });
    }
}
