using Servit.Domain.Enums;

namespace Servit.Domain.Entities;

public class ServiceRequestAttachment
{
    public Guid Id { get; set; }

    public Guid ServiceRequestId { get; set; }
    public ServiceRequest ServiceRequest { get; set; } = null!;

    public AttachmentType Type { get; set; }

    public required string StoragePath { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
