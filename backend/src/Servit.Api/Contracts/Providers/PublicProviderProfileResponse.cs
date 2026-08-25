using Servit.Api.Contracts.ServiceRequests;

namespace Servit.Api.Contracts.Providers;

public class PublicProviderProfileResponse
{
    public required Guid Id { get; set; }
    public required string FullName { get; set; }
    public string? Bio { get; set; }
    public required double AverageRating { get; set; }
    public required int RatingCount { get; set; }
    public required List<string> CategoryNames { get; set; }
    public required List<ReviewDto> Reviews { get; set; }
}
