namespace Servit.Api.Contracts.Admin.ServiceRequests;

public class AdminServiceRequestDetailDto
{
    public required Guid Id { get; set; }
    public required int CategoryId { get; set; }
    public required string CategoryName { get; set; }
    public required Guid CustomerId { get; set; }
    public required string CustomerName { get; set; }
    public required string CustomerEmail { get; set; }
    public required string Description { get; set; }
    public required double Lat { get; set; }
    public required double Lng { get; set; }
    public required string Status { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }

    public required IReadOnlyList<AdminAttachmentDto> Attachments { get; set; }
    public required IReadOnlyList<AdminQuoteDto> Quotes { get; set; }
    public required IReadOnlyList<AdminTimelineEventDto> Timeline { get; set; }
    public AdminRequestReviewDto? Review { get; set; }
}

public class AdminAttachmentDto
{
    public required Guid Id { get; set; }
    public required string Type { get; set; }
    public required string FileName { get; set; }
}

public class AdminQuoteDto
{
    public required Guid Id { get; set; }
    public required Guid ProviderId { get; set; }
    public required string ProviderName { get; set; }
    public string? Message { get; set; }
    public decimal? ProposedPrice { get; set; }
    public required string Status { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}

public class AdminTimelineEventDto
{
    public required string Type { get; set; }
    public DateTimeOffset? At { get; set; }
    public required string Description { get; set; }
}

public class AdminRequestReviewDto
{
    public required Guid Id { get; set; }
    public required int Rating { get; set; }
    public string? Comment { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
