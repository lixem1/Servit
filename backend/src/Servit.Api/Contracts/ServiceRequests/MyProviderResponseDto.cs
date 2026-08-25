namespace Servit.Api.Contracts.ServiceRequests;

public class MyProviderResponseDto
{
    public required Guid Id { get; set; }
    public string? Message { get; set; }
    public decimal? ProposedPrice { get; set; }
    public required string Status { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
