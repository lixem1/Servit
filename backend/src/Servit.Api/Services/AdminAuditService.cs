using System.Text.Json;
using Servit.Domain.Entities;
using Servit.Infrastructure.Persistence;

namespace Servit.Api.Services;

public class AdminAuditService(ServitDbContext dbContext) : IAdminAuditService
{
    public async Task LogAsync(
        Guid adminUserId,
        string action,
        string entityType,
        string entityId,
        object? metadata = null,
        object? oldValue = null,
        object? newValue = null,
        CancellationToken cancellationToken = default)
    {
        dbContext.AdminAuditLogs.Add(new AdminAuditLog
        {
            Id = Guid.NewGuid(),
            AdminUserId = adminUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            MetadataJson = Serialize(metadata) ?? "{}",
            OldValueJson = Serialize(oldValue),
            NewValueJson = Serialize(newValue),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? Serialize(object? value) =>
        value is null ? null : JsonSerializer.Serialize(value);
}
