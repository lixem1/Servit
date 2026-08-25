namespace Servit.Api.Contracts.ServiceRequests;

public class ProviderResponseDto
{
    public required Guid Id { get; set; }
    public required Guid ServiceRequestId { get; set; }
    public required Guid ProviderId { get; set; }
    public required string ProviderName { get; set; }
    public required double ProviderAverageRating { get; set; }
    public required int ProviderRatingCount { get; set; }
    public string? Message { get; set; }
    public decimal? ProposedPrice { get; set; }
    public required string Status { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
