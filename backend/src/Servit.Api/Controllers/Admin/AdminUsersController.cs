using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Servit.Api.Contracts.Admin.Common;
using Servit.Api.Contracts.Admin.Users;
using Servit.Api.Extensions;
using Servit.Api.Services;
using Servit.Domain.Constants;
using Servit.Domain.Entities;
using Servit.Domain.Enums;
using Servit.Infrastructure.Persistence;

namespace Servit.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/users")]
public class AdminUsersController(
    ServitDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IAdminAuditService audit,
    IEmailSender emailSender) : ControllerBase
{
    private static readonly string[] AssignableRoles = [Roles.Customer, Roles.Provider, Roles.Admin];
    private static readonly TimeSpan ResetCodeLifetime = TimeSpan.FromMinutes(15);

    [HttpGet]
    public async Task<ActionResult<PagedResponse<AdminUserDto>>> List([FromQuery] AdminUserListQuery query)
    {
        var users = dbContext.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            users = users.Where(u =>
                EF.Functions.ILike(u.FullName, $"%{term}%") ||
                EF.Functions.ILike(u.Email!, $"%{term}%"));
        }

        if (string.Equals(query.Status, "active", StringComparison.OrdinalIgnoreCase))
            users = users.Where(u => u.DeletedAt == null);
        else if (string.Equals(query.Status, "suspended", StringComparison.OrdinalIgnoreCase))
            users = users.Where(u => u.DeletedAt != null);

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var role = query.Role.Trim();
            users = users.Where(u =>
                (from ur in dbContext.UserRoles
                 join r in dbContext.Roles on ur.RoleId equals r.Id
                 where ur.UserId == u.Id
                 select r.Name).Contains(role));
        }

        if (query.CreatedFrom is not null) users = users.Where(u => u.CreatedAt >= query.CreatedFrom);
        if (query.CreatedTo is not null) users = users.Where(u => u.CreatedAt <= query.CreatedTo);

        users = query.Sort switch
        {
            "fullName" => users.OrderByField(u => u.FullName, query.Desc),
            "email" => users.OrderByField(u => u.Email, query.Desc),
            _ => users.OrderByField(u => u.CreatedAt, query.Desc || query.Sort is null),
        };

        var projected = users.Select(u => new AdminUserDto
        {
            Id = u.Id,
            Email = u.Email!,
            FullName = u.FullName,
            CreatedAt = u.CreatedAt,
            IsSuspended = u.DeletedAt != null,
            Roles = (from ur in dbContext.UserRoles
                     join r in dbContext.Roles on ur.RoleId equals r.Id
                     where ur.UserId == u.Id
                     select r.Name!).ToList(),
        });

        return Ok(await projected.ToPagedResponseAsync(query));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminUserDetailDto>> GetOne(Guid id)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        var roles = await userManager.GetRolesAsync(user);

        var provider = await dbContext.Providers
            .Include(p => p.ProviderCategories).ThenInclude(pc => pc.Category)
            .FirstOrDefaultAsync(p => p.UserId == id);

        var requests = await dbContext.ServiceRequests
            .Include(sr => sr.Category)
            .Include(sr => sr.Responses)
            .Where(sr => sr.CustomerId == id)
            .OrderByDescending(sr => sr.CreatedAt)
            .Take(50)
            .Select(sr => new AdminUserRequestDto
            {
                Id = sr.Id,
                CategoryName = sr.Category.Name,
                Status = sr.Status.ToString(),
                CreatedAt = sr.CreatedAt,
                ResponseCount = sr.Responses.Count,
            })
            .ToListAsync();

        var totalRequests = await dbContext.ServiceRequests.CountAsync(sr => sr.CustomerId == id);
        var completed = await dbContext.ServiceRequests.CountAsync(sr => sr.CustomerId == id && sr.Status == ServiceRequestStatus.Completed);
        var cancelled = await dbContext.ServiceRequests.CountAsync(sr => sr.CustomerId == id && sr.Status == ServiceRequestStatus.Cancelled);

        var reviewsGiven = await dbContext.Reviews
            .Include(r => r.Provider).ThenInclude(p => p.User)
            .Where(r => r.CustomerId == id)
            .OrderByDescending(r => r.CreatedAt)
            .Take(50)
            .Select(r => new AdminUserReviewDto
            {
                Id = r.Id,
                CounterpartName = r.Provider.User.FullName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
            })
            .ToListAsync();

        var reviewsReceived = provider is null
            ? new List<AdminUserReviewDto>()
            : await dbContext.Reviews
                .Include(r => r.Customer)
                .Where(r => r.ProviderId == provider.Id)
                .OrderByDescending(r => r.CreatedAt)
                .Take(50)
                .Select(r => new AdminUserReviewDto
                {
                    Id = r.Id,
                    CounterpartName = r.Customer.FullName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                })
                .ToListAsync();

        return Ok(new AdminUserDetailDto
        {
            Id = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            CreatedAt = user.CreatedAt,
            IsSuspended = user.DeletedAt != null,
            Roles = roles.ToList(),
            Provider = provider is null ? null : new AdminUserProviderDto
            {
                Id = provider.Id,
                Bio = provider.Bio,
                AverageRating = provider.AverageRating,
                RatingCount = provider.RatingCount,
                CategoryNames = provider.ProviderCategories.Select(pc => pc.Category.Name).ToList(),
            },
            TotalRequests = totalRequests,
            CompletedRequests = completed,
            CancelledRequests = cancelled,
            RecentRequests = requests,
            ReviewsGiven = reviewsGiven,
            ReviewsReceived = reviewsReceived,
        });
    }

    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();
        if (id == User.GetUserId()) return BadRequest("No puedes suspender tu propia cuenta.");
        if (user.DeletedAt is not null) return BadRequest("La cuenta ya está suspendida.");

        user.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        await audit.LogAsync(User.GetUserId(), "user.suspend", nameof(ApplicationUser), id.ToString(),
            metadata: new { user.Email });

        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();
        if (user.DeletedAt is null) return BadRequest("La cuenta no está suspendida.");

        user.DeletedAt = null;
        await dbContext.SaveChangesAsync();

        await audit.LogAsync(User.GetUserId(), "user.restore", nameof(ApplicationUser), id.ToString(),
            metadata: new { user.Email });

        return NoContent();
    }

    [HttpPost("{id:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, ChangeRoleRequest request)
    {
        if (!AssignableRoles.Contains(request.Role))
        {
            return BadRequest($"El rol debe ser uno de: {string.Join(", ", AssignableRoles)}.");
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        var currentRoles = await userManager.GetRolesAsync(user);
        if (id == User.GetUserId() && currentRoles.Contains(Roles.Admin) && request.Role != Roles.Admin)
        {
            return BadRequest("No puedes quitarte a ti mismo el rol Admin.");
        }

        if (currentRoles.Count > 0)
        {
            await userManager.RemoveFromRolesAsync(user, currentRoles);
        }
        await userManager.AddToRoleAsync(user, request.Role);

        // Keep the Provider profile row in sync when promoting to Provider.
        if (request.Role == Roles.Provider && !await dbContext.Providers.AnyAsync(p => p.UserId == id))
        {
            dbContext.Providers.Add(new Provider { UserId = id });
            await dbContext.SaveChangesAsync();
        }

        await audit.LogAsync(User.GetUserId(), "user.role", nameof(ApplicationUser), id.ToString(),
            oldValue: new { Roles = currentRoles }, newValue: new { Role = request.Role });

        return NoContent();
    }

    [HttpPost("{id:guid}/force-reset")]
    public async Task<IActionResult> ForceReset(Guid id)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        // Reuse the self-service reset flow: issue a fresh code and email it to the user.
        var existing = dbContext.PasswordResetCodes.Where(c => c.UserId == user.Id);
        dbContext.PasswordResetCodes.RemoveRange(existing);

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        dbContext.PasswordResetCodes.Add(new PasswordResetCode
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CodeHash = HashCode(code),
            ExpiresAt = DateTimeOffset.UtcNow.Add(ResetCodeLifetime),
        });
        await dbContext.SaveChangesAsync();

        await emailSender.SendAsync(
            user.Email!,
            "Restablecimiento de contraseña de Servit",
            $"Un administrador solicitó restablecer tu contraseña.\n\nTu código es: {code}\n\nVence en 15 minutos.");

        await audit.LogAsync(User.GetUserId(), "user.force-reset", nameof(ApplicationUser), id.ToString(),
            metadata: new { user.Email });

        return NoContent();
    }

    private static string HashCode(string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
}
