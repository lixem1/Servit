using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Servit.Api.Contracts.Admin.Categories;
using Servit.Api.Contracts.Admin.Common;
using Servit.Api.Extensions;
using Servit.Api.Services;
using Servit.Domain.Constants;
using Servit.Domain.Entities;
using Servit.Infrastructure.Persistence;

namespace Servit.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/categories")]
public class AdminCategoriesController(ServitDbContext dbContext, IAdminAuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AdminCategoryDto>>> List([FromQuery] AdminCategoryListQuery query)
    {
        var categories = dbContext.Categories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            categories = categories.Where(c => EF.Functions.ILike(c.Name, $"%{term}%"));
        }

        categories = query.Sort switch
        {
            "name" => categories.OrderByField(c => c.Name, query.Desc),
            _ => categories.OrderByField(c => c.Id, query.Desc),
        };

        var projected = categories.Select(c => new AdminCategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            ProviderCount = c.ProviderCategories.Count,
            RequestCount = dbContext.ServiceRequests.Count(sr => sr.CategoryId == c.Id),
        });

        return Ok(await projected.ToPagedResponseAsync(query));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminCategoryDto>> GetOne(int id)
    {
        var dto = await dbContext.Categories
            .Where(c => c.Id == id)
            .Select(c => new AdminCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ProviderCount = c.ProviderCategories.Count,
                RequestCount = dbContext.ServiceRequests.Count(sr => sr.CategoryId == c.Id),
            })
            .FirstOrDefaultAsync();

        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<AdminCategoryDto>> Create(SaveCategoryRequest request)
    {
        var name = request.Name.Trim();
        if (await dbContext.Categories.AnyAsync(c => c.Name == name))
        {
            return Conflict("Ya existe una categoría con ese nombre.");
        }

        var category = new Category { Name = name, Description = request.Description };
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();

        await audit.LogAsync(User.GetUserId(), "category.create", nameof(Category), category.Id.ToString(),
            metadata: new { category.Name });

        return CreatedAtAction(nameof(GetOne), new { id = category.Id }, ToDto(category, 0, 0));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AdminCategoryDto>> Update(int id, SaveCategoryRequest request)
    {
        var category = await dbContext.Categories.FindAsync(id);
        if (category is null) return NotFound();

        var name = request.Name.Trim();
        if (await dbContext.Categories.AnyAsync(c => c.Name == name && c.Id != id))
        {
            return Conflict("Ya existe una categoría con ese nombre.");
        }

        var old = new { category.Name, category.Description };
        category.Name = name;
        category.Description = request.Description;
        await dbContext.SaveChangesAsync();

        await audit.LogAsync(User.GetUserId(), "category.update", nameof(Category), category.Id.ToString(),
            oldValue: old, newValue: new { category.Name, category.Description });

        return Ok(ToDto(
            category,
            await dbContext.ProviderCategories.CountAsync(pc => pc.CategoryId == id),
            await dbContext.ServiceRequests.CountAsync(sr => sr.CategoryId == id)));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await dbContext.Categories.FindAsync(id);
        if (category is null) return NotFound();

        // Safe delete: block while providers or requests still reference the category.
        var providerCount = await dbContext.ProviderCategories.CountAsync(pc => pc.CategoryId == id);
        var requestCount = await dbContext.ServiceRequests.CountAsync(sr => sr.CategoryId == id);
        if (providerCount > 0 || requestCount > 0)
        {
            return Conflict(
                $"No se puede eliminar: la categoría tiene {providerCount} proveedor(es) y {requestCount} solicitud(es) asociada(s).");
        }

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync();

        await audit.LogAsync(User.GetUserId(), "category.delete", nameof(Category), id.ToString(),
            oldValue: new { category.Name, category.Description });

        return NoContent();
    }

    private static AdminCategoryDto ToDto(Category c, int providerCount, int requestCount) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        ProviderCount = providerCount,
        RequestCount = requestCount,
    };
}
