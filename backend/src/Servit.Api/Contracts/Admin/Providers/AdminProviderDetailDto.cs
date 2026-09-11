namespace Servit.Api.Contracts.Admin.Providers;

// 360 view of a provider: performance metrics for the admin panel.
public class AdminProviderDetailDto
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? Bio { get; set; }
    public required bool IsSuspended { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }

    public required double AverageRating { get; set; }
    public required int RatingCount { get; set; }
    public required IReadOnlyList<string> CategoryNames { get; set; }

    public required int CompletedJobs { get; set; }
    public required int TotalResponses { get; set; }
    public required int AcceptedResponses { get; set; }
    // accepted / total responses, 0..1.
    public required double AcceptanceRate { get; set; }
    // Mean seconds between a request being created and this provider responding; null if none.
    public double? AverageResponseSeconds { get; set; }
    // Sum of accepted ProposedPrice on completed requests.
    public required decimal GmvGenerated { get; set; }
}
