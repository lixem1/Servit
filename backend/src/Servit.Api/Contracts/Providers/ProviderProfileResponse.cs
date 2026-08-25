namespace Servit.Api.Contracts.Providers;

public class ProviderProfileResponse
{
    public required Guid Id { get; set; }
    public string? Bio { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public required double AverageRating { get; set; }
    public required int RatingCount { get; set; }
    public required List<int> CategoryIds { get; set; }
}
