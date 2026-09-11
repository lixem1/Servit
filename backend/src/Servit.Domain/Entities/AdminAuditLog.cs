namespace Servit.Domain.Entities;

// Immutable record of a mutating action performed by an administrator. Written by
// AdminAuditService on every admin mutation so the panel can show a full audit trail.
public class AdminAuditLog
{
    public Guid Id { get; set; }

    public Guid AdminUserId { get; set; }
    public ApplicationUser? AdminUser { get; set; }

    // e.g. "user.suspend", "category.delete".
    public required string Action { get; set; }

    // Target entity, kept as strings so any PK type is supported.
    public required string EntityType { get; set; }
    public required string EntityId { get; set; }

    // Free-form JSON context ("{}" when there is none).
    public required string MetadataJson { get; set; }

    // Before/after snapshots (JSON) when the action changes existing data.
    public string? OldValueJson { get; set; }
    public string? NewValueJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
