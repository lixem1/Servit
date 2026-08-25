namespace Servit.Api.Contracts.ServiceRequests;

public class ServiceRequestDto
{
    public required Guid Id { get; set; }
    public required int CategoryId { get; set; }
    public required string CategoryName { get; set; }
    public required string Description { get; set; }
    public required double Lat { get; set; }
    public required double Lng { get; set; }
    public required string Status { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public double? DistanceMeters { get; set; }
    public required List<AttachmentDto> Attachments { get; set; }
    public bool HasReview { get; set; }
    public int ResponseCount { get; set; }
    public MyProviderResponseDto? MyResponse { get; set; }
    public string? CustomerName { get; set; }
    public int? Rating { get; set; }
    public string? ReviewComment { get; set; }
}
