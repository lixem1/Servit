using NetTopologySuite.Geometries;

namespace Servit.Domain.Entities;

public class Provider
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string? Bio { get; set; }

    // SRID 4326 (WGS84) so PostGIS treats this as lon/lat geography.
    public Point? Location { get; set; }

    public double AverageRating { get; set; }
    public int RatingCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ProviderCategory> ProviderCategories { get; set; } = new List<ProviderCategory>();
}
