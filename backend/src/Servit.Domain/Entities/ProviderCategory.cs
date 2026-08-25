namespace Servit.Domain.Entities;

public class ProviderCategory
{
    public Guid ProviderId { get; set; }
    public Provider Provider { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
