namespace Servit.Api.Contracts.Admin.Reviews;

public class AdminReviewDto
{
    public required Guid Id { get; set; }
    public required Guid ServiceRequestId { get; set; }
    public required Guid ProviderId { get; set; }
    public required string ProviderName { get; set; }
    public required Guid CustomerId { get; set; }
    public required string CustomerName { get; set; }
    public required int Rating { get; set; }
    public string? Comment { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
