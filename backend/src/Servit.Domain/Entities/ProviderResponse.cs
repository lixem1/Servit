using Servit.Domain.Enums;

namespace Servit.Domain.Entities;

public class ProviderResponse
{
    public Guid Id { get; set; }

    public Guid ServiceRequestId { get; set; }
    public ServiceRequest ServiceRequest { get; set; } = null!;

    public Guid ProviderId { get; set; }
    public Provider Provider { get; set; } = null!;

    public string? Message { get; set; }
    public decimal? ProposedPrice { get; set; }

    public ProviderResponseStatus Status { get; set; } = ProviderResponseStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
