using System.ComponentModel.DataAnnotations;

namespace Servit.Api.Contracts.Admin.Categories;

public class SaveCategoryRequest
{
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}
