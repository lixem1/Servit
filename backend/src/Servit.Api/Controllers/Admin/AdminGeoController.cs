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
    // Coordinates are read in-memory: the columns are `geography`, and PostgreSQL's ST_X/ST_Y
    // are geometry-only, so projecting .X/.Y in SQL fails. Npgsql materializes the Point instead.
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
            .Select(sr => new
            {
                sr.Id,
                sr.Location,
                sr.Status,
                sr.CategoryId,
                CategoryName = sr.Category.Name,
                sr.CreatedAt,
            })
            .ToListAsync();

        var result = rows.Select(r => new GeoRequestDto
        {
            Id = r.Id,
            Lat = r.Location.Y,
            Lng = r.Location.X,
            Status = r.Status,
            CategoryId = r.CategoryId,
            CategoryName = r.CategoryName,
            CreatedAt = r.CreatedAt,
        }).ToList();

        return Ok(result);
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
            .Select(p => new
            {
                p.Id,
                p.Location,
                FullName = p.User.FullName,
                p.AverageRating,
                p.RatingCount,
            })
            .ToListAsync();

        var result = rows.Select(p => new GeoProviderDto
        {
            Id = p.Id,
            Lat = p.Location!.Y,
            Lng = p.Location!.X,
            FullName = p.FullName,
            AverageRating = p.AverageRating,
            RatingCount = p.RatingCount,
        }).ToList();

        return Ok(result);
    }
}
