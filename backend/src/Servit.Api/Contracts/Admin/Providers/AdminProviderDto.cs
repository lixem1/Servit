namespace Servit.Api.Contracts.Admin.Providers;

public class AdminProviderDto
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required bool IsSuspended { get; set; }
    public required double AverageRating { get; set; }
    public required int RatingCount { get; set; }
    public required int CategoryCount { get; set; }
    public required int CompletedJobs { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
