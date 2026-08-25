using System.Security.Cryptography;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Servit.Api.Contracts.Auth;
using Servit.Api.Services;
using Servit.Domain.Constants;
using Servit.Domain.Entities;
using Servit.Infrastructure.Persistence;

namespace Servit.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    JwtTokenService tokenService,
    ServitDbContext dbContext,
    IEmailSender emailSender,
    IConfiguration configuration) : ControllerBase
{
    private static readonly string[] AllowedRoles = [Roles.Customer, Roles.Provider];
    private static readonly TimeSpan ResetCodeLifetime = TimeSpan.FromMinutes(15);

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (!AllowedRoles.Contains(request.Role))
        {
            return BadRequest($"Role must be one of: {string.Join(", ", AllowedRoles)}");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        if (!await roleManager.RoleExistsAsync(request.Role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(request.Role));
        }
        await userManager.AddToRoleAsync(user, request.Role);

        if (request.Role == Roles.Provider)
        {
            dbContext.Providers.Add(new Provider { UserId = user.Id });
            await dbContext.SaveChangesAsync();
        }

        var (token, expiresAt) = tokenService.CreateToken(user, [request.Role]);
        return Ok(new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            FullName = user.FullName,
            Role = request.Role
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || user.DeletedAt is not null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized("Invalid email or password.");
        }

        var roles = await userManager.GetRolesAsync(user);
        var (token, expiresAt) = tokenService.CreateToken(user, roles);
        return Ok(new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            FullName = user.FullName,
            Role = roles.FirstOrDefault() ?? string.Empty
        });
    }

    [HttpPost("google")]
    public async Task<ActionResult<GoogleAuthResponse>> Google(GoogleAuthRequest request)
    {
        var iosClientId = configuration["GoogleAuth:IosClientId"];
        if (string.IsNullOrEmpty(iosClientId))
        {
            throw new InvalidOperationException("GoogleAuth:IosClientId is not configured. Run 'dotnet user-secrets set GoogleAuth:IosClientId <value>'.");
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [iosClientId]
            });
        }
        catch (InvalidJwtException)
        {
            return Unauthorized("Invalid Google token.");
        }

        var user = await userManager.FindByEmailAsync(payload.Email);
        if (user is not null)
        {
            if (user.DeletedAt is not null)
            {
                return Unauthorized("Invalid Google token.");
            }

            var existingRoles = await userManager.GetRolesAsync(user);
            var (existingToken, existingExpiresAt) = tokenService.CreateToken(user, existingRoles);
            return Ok(new GoogleAuthResponse
            {
                RequiresRole = false,
                Auth = new AuthResponse
                {
                    Token = existingToken,
                    ExpiresAt = existingExpiresAt,
                    UserId = user.Id,
                    FullName = user.FullName,
                    Role = existingRoles.FirstOrDefault() ?? string.Empty
                }
            });
        }

        if (string.IsNullOrEmpty(request.Role))
        {
            return Ok(new GoogleAuthResponse
            {
                RequiresRole = true,
                Email = payload.Email,
                FullName = payload.Name
            });
        }

        if (!AllowedRoles.Contains(request.Role))
        {
            return BadRequest($"Role must be one of: {string.Join(", ", AllowedRoles)}");
        }

        var newUser = new ApplicationUser
        {
            UserName = payload.Email,
            Email = payload.Email,
            FullName = payload.Name ?? payload.Email
        };

        var createResult = await userManager.CreateAsync(newUser);
        if (!createResult.Succeeded)
        {
            return BadRequest(createResult.Errors.Select(e => e.Description));
        }

        if (!await roleManager.RoleExistsAsync(request.Role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(request.Role));
        }
        await userManager.AddToRoleAsync(newUser, request.Role);

        if (request.Role == Roles.Provider)
        {
            dbContext.Providers.Add(new Provider { UserId = newUser.Id });
            await dbContext.SaveChangesAsync();
        }

        var (newToken, newExpiresAt) = tokenService.CreateToken(newUser, [request.Role]);
        return Ok(new GoogleAuthResponse
        {
            RequiresRole = false,
            Auth = new AuthResponse
            {
                Token = newToken,
                ExpiresAt = newExpiresAt,
                UserId = newUser.Id,
                FullName = newUser.FullName,
                Role = request.Role
            }
        });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is not null && user.DeletedAt is null)
        {
            var existingCodes = dbContext.PasswordResetCodes.Where(c => c.UserId == user.Id);
            dbContext.PasswordResetCodes.RemoveRange(existingCodes);

            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            dbContext.PasswordResetCodes.Add(new PasswordResetCode
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CodeHash = HashCode(code),
                ExpiresAt = DateTimeOffset.UtcNow.Add(ResetCodeLifetime)
            });
            await dbContext.SaveChangesAsync();

            await emailSender.SendAsync(
                user.Email!,
                "Código para restablecer tu contraseña de Servit",
                $"Tu código para restablecer tu contraseña es: {code}\n\nEste código vence en 15 minutos. Si no solicitaste esto, ignora este correo.");
        }

        // Always return 200 so this endpoint can't be used to check which emails are registered.
        return Ok();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || user.DeletedAt is not null)
        {
            return BadRequest("Invalid or expired code.");
        }

        var codeHash = HashCode(request.Code);
        var resetCode = await dbContext.PasswordResetCodes
            .Where(c => c.UserId == user.Id && c.CodeHash == codeHash)
            .FirstOrDefaultAsync();
        if (resetCode is null || resetCode.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return BadRequest("Invalid or expired code.");
        }

        if (await userManager.HasPasswordAsync(user))
        {
            await userManager.RemovePasswordAsync(user);
        }
        var result = await userManager.AddPasswordAsync(user, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        dbContext.PasswordResetCodes.Remove(resetCode);
        await dbContext.SaveChangesAsync();

        return Ok();
    }

    private static string HashCode(string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
}
