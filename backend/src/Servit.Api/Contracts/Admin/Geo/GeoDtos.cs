using Servit.Domain.Enums;

namespace Servit.Api.Contracts.Admin.Geo;

public class GeoRequestDto
{
    public required Guid Id { get; set; }
    public required double Lat { get; set; }
    public required double Lng { get; set; }
    public required ServiceRequestStatus Status { get; set; }
    public required int CategoryId { get; set; }
    public required string CategoryName { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}

public class GeoProviderDto
{
    public required Guid Id { get; set; }
    public required double Lat { get; set; }
    public required double Lng { get; set; }
    public required string FullName { get; set; }
    public required double AverageRating { get; set; }
    public required int RatingCount { get; set; }
}
