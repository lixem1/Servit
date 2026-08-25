namespace Servit.Domain.Entities;

public class Review
{
    public Guid Id { get; set; }

    public Guid ServiceRequestId { get; set; }
    public ServiceRequest ServiceRequest { get; set; } = null!;

    public Guid ProviderId { get; set; }
    public Provider Provider { get; set; } = null!;

    public Guid CustomerId { get; set; }
    public ApplicationUser Customer { get; set; } = null!;

    public int Rating { get; set; }
    public string? Comment { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
