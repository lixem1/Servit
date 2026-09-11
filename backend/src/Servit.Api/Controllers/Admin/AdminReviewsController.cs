using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Servit.Api.Contracts.Admin.Common;
using Servit.Api.Contracts.Admin.Reviews;
using Servit.Api.Extensions;
using Servit.Api.Services;
using Servit.Domain.Constants;
using Servit.Domain.Entities;
using Servit.Infrastructure.Persistence;

namespace Servit.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/reviews")]
public class AdminReviewsController(ServitDbContext dbContext, IAdminAuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AdminReviewDto>>> List([FromQuery] AdminReviewListQuery query)
    {
        var reviews = dbContext.Reviews.AsQueryable();

        if (query.ProviderId is not null) reviews = reviews.Where(r => r.ProviderId == query.ProviderId);
        if (query.Rating is not null) reviews = reviews.Where(r => r.Rating == query.Rating);
        if (query.DateFrom is not null) reviews = reviews.Where(r => r.CreatedAt >= query.DateFrom);
        if (query.DateTo is not null) reviews = reviews.Where(r => r.CreatedAt <= query.DateTo);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            reviews = reviews.Where(r => r.Comment != null && EF.Functions.ILike(r.Comment, $"%{term}%"));
        }

        reviews = query.Sort switch
        {
            "rating" => reviews.OrderByField(r => r.Rating, query.Desc),
            _ => reviews.OrderByField(r => r.CreatedAt, query.Desc || query.Sort is null),
        };

        var projected = reviews.Select(r => new AdminReviewDto
        {
            Id = r.Id,
            ServiceRequestId = r.ServiceRequestId,
            ProviderId = r.ProviderId,
            ProviderName = r.Provider.User.FullName,
            CustomerId = r.CustomerId,
            CustomerName = r.Customer.FullName,
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt,
        });

        return Ok(await projected.ToPagedResponseAsync(query));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var review = await dbContext.Reviews.FindAsync(id);
        if (review is null) return NotFound();

        var providerId = review.ProviderId;
        dbContext.Reviews.Remove(review);
        await dbContext.SaveChangesAsync();

        // Recalculate the provider's rating from the remaining reviews.
        var provider = await dbContext.Providers.FindAsync(providerId);
        if (provider is not null)
        {
            var ratings = await dbContext.Reviews
                .Where(r => r.ProviderId == providerId)
                .Select(r => r.Rating)
                .ToListAsync();
            provider.RatingCount = ratings.Count;
            provider.AverageRating = ratings.Count == 0 ? 0 : ratings.Average();
            await dbContext.SaveChangesAsync();
        }

        await audit.LogAsync(User.GetUserId(), "review.delete", nameof(Review), id.ToString(),
            oldValue: new { review.ProviderId, review.CustomerId, review.Rating, review.Comment });

        return NoContent();
    }
}
