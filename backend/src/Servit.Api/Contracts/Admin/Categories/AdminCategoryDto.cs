namespace Servit.Api.Contracts.Admin.Categories;

public class AdminCategoryDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required int ProviderCount { get; set; }
    public required int RequestCount { get; set; }
}
