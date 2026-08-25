namespace Servit.Api.Contracts.Categories;

public class CategoryDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
