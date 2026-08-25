namespace Servit.Api.Contracts.ServiceRequests;

public class AttachmentDto
{
    public required Guid Id { get; set; }
    public required string Type { get; set; }
    public required string FileName { get; set; }
}
