using NetTopologySuite.Geometries;
using Servit.Domain.Enums;

namespace Servit.Domain.Entities;

public class ServiceRequest
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }
    public ApplicationUser Customer { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public required string Description { get; set; }

    // SRID 4326 (WGS84) so PostGIS treats this as lon/lat geography.
    public required Point Location { get; set; }

    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ProviderResponse> Responses { get; set; } = new List<ProviderResponse>();
    public ICollection<ServiceRequestAttachment> Attachments { get; set; } = new List<ServiceRequestAttachment>();
}
