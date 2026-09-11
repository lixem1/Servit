using Servit.Api.Contracts.Admin.Common;

namespace Servit.Api.Contracts.Admin.Audit;

public class AdminAuditLogDto
{
    public required Guid Id { get; set; }
    public required Guid AdminUserId { get; set; }
    public string? AdminUserName { get; set; }
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public required string EntityId { get; set; }
    public required string MetadataJson { get; set; }
    public string? OldValueJson { get; set; }
    public string? NewValueJson { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}

public class AdminAuditLogListQuery : PagedQuery
{
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public Guid? AdminUserId { get; set; }
    public DateTimeOffset? DateFrom { get; set; }
    public DateTimeOffset? DateTo { get; set; }
    // Free-text over action or entityId.
    public string? Search { get; set; }
}
