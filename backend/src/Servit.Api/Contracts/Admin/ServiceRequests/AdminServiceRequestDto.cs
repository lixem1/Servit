namespace Servit.Api.Contracts.Admin.ServiceRequests;

public class AdminServiceRequestDto
{
    public required Guid Id { get; set; }
    public required string CategoryName { get; set; }
    public required int CategoryId { get; set; }
    public required Guid CustomerId { get; set; }
    public required string CustomerName { get; set; }
    public required string Description { get; set; }
    public required string Status { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required int ResponseCount { get; set; }
    public string? AcceptedProviderName { get; set; }
}
