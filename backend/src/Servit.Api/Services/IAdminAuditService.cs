namespace Servit.Api.Services;

// Records administrator actions to the audit log. Inject into any admin controller
// and call after a successful mutation. metadata/oldValue/newValue are serialized to JSON.
public interface IAdminAuditService
{
    Task LogAsync(
        Guid adminUserId,
        string action,
        string entityType,
        string entityId,
        object? metadata = null,
        object? oldValue = null,
        object? newValue = null,
        CancellationToken cancellationToken = default);
}
