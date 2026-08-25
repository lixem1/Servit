namespace Servit.Api.Contracts.ServiceRequests;

public class ReviewDto
{
    public required Guid Id { get; set; }
    public required string CustomerName { get; set; }
    public required int Rating { get; set; }
    public string? Comment { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
