using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Servit.Api.Contracts.Admin.Common;
using Servit.Api.Contracts.Admin.Providers;
using Servit.Api.Extensions;
using Servit.Domain.Constants;
using Servit.Domain.Enums;
using Servit.Infrastructure.Persistence;

namespace Servit.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/providers")]
public class AdminProvidersController(ServitDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AdminProviderDto>>> List([FromQuery] AdminProviderListQuery query)
    {
        var providers = dbContext.Providers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            providers = providers.Where(p =>
                EF.Functions.ILike(p.User.FullName, $"%{term}%") ||
                EF.Functions.ILike(p.User.Email!, $"%{term}%"));
        }

        if (query.CategoryId is not null)
        {
            providers = providers.Where(p => p.ProviderCategories.Any(pc => pc.CategoryId == query.CategoryId));
        }

        providers = query.Sort switch
        {
            "fullName" => providers.OrderByField(p => p.User.FullName, query.Desc),
            "averageRating" => providers.OrderByField(p => p.AverageRating, query.Desc),
            "ratingCount" => providers.OrderByField(p => p.RatingCount, query.Desc),
            _ => providers.OrderByField(p => p.CreatedAt, query.Desc || query.Sort is null),
        };

        var projected = providers.Select(p => new AdminProviderDto
        {
            Id = p.Id,
            UserId = p.UserId,
            FullName = p.User.FullName,
            Email = p.User.Email!,
            IsSuspended = p.User.DeletedAt != null,
            AverageRating = p.AverageRating,
            RatingCount = p.RatingCount,
            CategoryCount = p.ProviderCategories.Count,
            CompletedJobs = dbContext.ProviderResponses.Count(r =>
                r.ProviderId == p.Id &&
                r.Status == ProviderResponseStatus.Accepted &&
                r.ServiceRequest.Status == ServiceRequestStatus.Completed),
            CreatedAt = p.CreatedAt,
        });

        return Ok(await projected.ToPagedResponseAsync(query));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminProviderDetailDto>> GetOne(Guid id)
    {
        var provider = await dbContext.Providers
            .Include(p => p.User)
            .Include(p => p.ProviderCategories).ThenInclude(pc => pc.Category)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (provider is null) return NotFound();

        var totalResponses = await dbContext.ProviderResponses.CountAsync(r => r.ProviderId == id);
        var acceptedResponses = await dbContext.ProviderResponses
            .CountAsync(r => r.ProviderId == id && r.Status == ProviderResponseStatus.Accepted);
        var completedJobs = await dbContext.ProviderResponses.CountAsync(r =>
            r.ProviderId == id &&
            r.Status == ProviderResponseStatus.Accepted &&
            r.ServiceRequest.Status == ServiceRequestStatus.Completed);

        var gmv = await dbContext.ProviderResponses
            .Where(r => r.ProviderId == id &&
                r.Status == ProviderResponseStatus.Accepted &&
                r.ServiceRequest.Status == ServiceRequestStatus.Completed)
            .SumAsync(r => (decimal?)r.ProposedPrice) ?? 0m;

        var responseTimes = await dbContext.ProviderResponses
            .Where(r => r.ProviderId == id)
            .Select(r => new { r.CreatedAt, RequestCreatedAt = r.ServiceRequest.CreatedAt })
            .ToListAsync();
        double? avgResponseSeconds = responseTimes.Count == 0
            ? null
            : responseTimes.Average(x => (x.CreatedAt - x.RequestCreatedAt).TotalSeconds);

        return Ok(new AdminProviderDetailDto
        {
            Id = provider.Id,
            UserId = provider.UserId,
            FullName = provider.User.FullName,
            Email = provider.User.Email!,
            Bio = provider.Bio,
            IsSuspended = provider.User.DeletedAt != null,
            CreatedAt = provider.CreatedAt,
            AverageRating = provider.AverageRating,
            RatingCount = provider.RatingCount,
            CategoryNames = provider.ProviderCategories.Select(pc => pc.Category.Name).ToList(),
            CompletedJobs = completedJobs,
            TotalResponses = totalResponses,
            AcceptedResponses = acceptedResponses,
            AcceptanceRate = totalResponses == 0 ? 0 : (double)acceptedResponses / totalResponses,
            AverageResponseSeconds = avgResponseSeconds,
            GmvGenerated = gmv,
        });
    }
}
