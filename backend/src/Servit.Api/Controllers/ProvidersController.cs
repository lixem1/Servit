using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Servit.Api.Contracts.Providers;
using Servit.Api.Contracts.ServiceRequests;
using Servit.Api.Extensions;
using Servit.Domain.Constants;
using Servit.Domain.Entities;
using Servit.Infrastructure.Persistence;

namespace Servit.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/providers")]
public class ProvidersController(ServitDbContext dbContext) : ControllerBase
{
    [HttpGet("me")]
    [Authorize(Roles = Roles.Provider)]
    public async Task<ActionResult<ProviderProfileResponse>> GetMe()
    {
        var provider = await FindProvider();
        if (provider is null) return NotFound();

        return Ok(ToResponse(provider));
    }

    [HttpPut("me/categories")]
    [Authorize(Roles = Roles.Provider)]
    public async Task<ActionResult<ProviderProfileResponse>> UpdateCategories(UpdateProviderCategoriesRequest request)
    {
        var provider = await FindProvider();
        if (provider is null) return NotFound();

        var validCategoryIds = await dbContext.Categories
            .Where(c => request.CategoryIds.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync();

        dbContext.ProviderCategories.RemoveRange(provider.ProviderCategories);
        provider.ProviderCategories = validCategoryIds
            .Select(categoryId => new ProviderCategory { ProviderId = provider.Id, CategoryId = categoryId })
            .ToList();

        await dbContext.SaveChangesAsync();
        return Ok(ToResponse(provider));
    }

    [HttpPut("me/location")]
    [Authorize(Roles = Roles.Provider)]
    public async Task<ActionResult<ProviderProfileResponse>> UpdateLocation(UpdateProviderLocationRequest request)
    {
        var provider = await FindProvider();
        if (provider is null) return NotFound();

        provider.Location = new Point(request.Lng, request.Lat) { SRID = 4326 };
        await dbContext.SaveChangesAsync();
        return Ok(ToResponse(provider));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PublicProviderProfileResponse>> GetPublicProfile(Guid id)
    {
        var provider = await dbContext.Providers
            .Include(p => p.User)
            .Include(p => p.ProviderCategories).ThenInclude(pc => pc.Category)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (provider is null) return NotFound();

        var reviews = await dbContext.Reviews
            .Include(r => r.Customer)
            .Where(r => r.ProviderId == id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(new PublicProviderProfileResponse
        {
            Id = provider.Id,
            FullName = provider.User.FullName,
            Bio = provider.Bio,
            AverageRating = provider.AverageRating,
            RatingCount = provider.RatingCount,
            CategoryNames = provider.ProviderCategories.Select(pc => pc.Category.Name).ToList(),
            Reviews = reviews.Select(r => new ReviewDto
            {
                Id = r.Id,
                CustomerName = r.Customer.FullName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList()
        });
    }

    private Task<Provider?> FindProvider()
    {
        var userId = User.GetUserId();
        return dbContext.Providers
            .Include(p => p.ProviderCategories)
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    private static ProviderProfileResponse ToResponse(Provider provider) => new()
    {
        Id = provider.Id,
        Bio = provider.Bio,
        Lat = provider.Location?.Y,
        Lng = provider.Location?.X,
        AverageRating = provider.AverageRating,
        RatingCount = provider.RatingCount,
        CategoryIds = provider.ProviderCategories.Select(pc => pc.CategoryId).ToList()
    };
}
