using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Servit.Api.Contracts.Admin.Geo;
using Servit.Domain.Constants;
using Servit.Domain.Enums;
using Servit.Infrastructure.Persistence;

namespace Servit.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/geo")]
public class AdminGeoController(ServitDbContext dbContext) : ControllerBase
{
    // Service-request points for a demand map/heatmap. Point.Y = lat, Point.X = lng (SRID 4326).
    [HttpGet("requests")]
    public async Task<ActionResult<List<GeoRequestDto>>> Requests(
        [FromQuery] ServiceRequestStatus? status,
        [FromQuery] int? categoryId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to)
    {
        var query = dbContext.ServiceRequests.AsQueryable();
        if (status is not null) query = query.Where(sr => sr.Status == status);
        if (categoryId is not null) query = query.Where(sr => sr.CategoryId == categoryId);
        if (from is not null) query = query.Where(sr => sr.CreatedAt >= from);
        if (to is not null) query = query.Where(sr => sr.CreatedAt <= to);

        var rows = await query
            .Select(sr => new GeoRequestDto
            {
                Id = sr.Id,
                Lat = sr.Location.Y,
                Lng = sr.Location.X,
                Status = sr.Status,
                CategoryId = sr.CategoryId,
                CategoryName = sr.Category.Name,
                CreatedAt = sr.CreatedAt,
            })
            .ToListAsync();

        return Ok(rows);
    }

    // Provider points (only those with a known location), optionally by category.
    [HttpGet("providers")]
    public async Task<ActionResult<List<GeoProviderDto>>> Providers([FromQuery] int? categoryId)
    {
        var query = dbContext.Providers.Where(p => p.Location != null);
        if (categoryId is not null)
        {
            query = query.Where(p => p.ProviderCategories.Any(pc => pc.CategoryId == categoryId));
        }

        var rows = await query
            .Select(p => new GeoProviderDto
            {
                Id = p.Id,
                Lat = p.Location!.Y,
                Lng = p.Location!.X,
                FullName = p.User.FullName,
                AverageRating = p.AverageRating,
                RatingCount = p.RatingCount,
            })
            .ToListAsync();

        return Ok(rows);
    }
}
