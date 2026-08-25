using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Servit.Api.Contracts.Account;
using Servit.Api.Extensions;
using Servit.Domain.Entities;

namespace Servit.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/account")]
public class AccountController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<AccountProfileResponse>> GetMe()
    {
        var user = await FindUser();
        if (user is null) return NotFound();

        return Ok(await ToResponse(user));
    }

    [HttpPut("me")]
    public async Task<ActionResult<AccountProfileResponse>> UpdateMe(UpdateProfileRequest request)
    {
        var user = await FindUser();
        if (user is null) return NotFound();

        user.FullName = request.FullName;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        return Ok(await ToResponse(user));
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var user = await FindUser();
        if (user is null) return NotFound();

        var hasPassword = await userManager.HasPasswordAsync(user);
        IdentityResult result;
        if (hasPassword)
        {
            if (string.IsNullOrEmpty(request.CurrentPassword))
            {
                return BadRequest("CurrentPassword is required.");
            }
            result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        }
        else
        {
            result = await userManager.AddPasswordAsync(user, request.NewPassword);
        }

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        return NoContent();
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe()
    {
        var user = await FindUser();
        if (user is null) return NotFound();

        user.DeletedAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        return NoContent();
    }

    private Task<ApplicationUser?> FindUser() => userManager.FindByIdAsync(User.GetUserId().ToString());

    private async Task<AccountProfileResponse> ToResponse(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new AccountProfileResponse
        {
            FullName = user.FullName,
            Email = user.Email!,
            Role = roles.FirstOrDefault() ?? string.Empty,
            HasPassword = await userManager.HasPasswordAsync(user)
        };
    }
}
