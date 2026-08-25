namespace Servit.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    public ICollection<ProviderCategory> ProviderCategories { get; set; } = new List<ProviderCategory>();
}
