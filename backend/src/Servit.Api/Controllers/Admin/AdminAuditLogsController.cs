using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Servit.Api.Contracts.Admin.Audit;
using Servit.Api.Contracts.Admin.Common;
using Servit.Api.Extensions;
using Servit.Domain.Constants;
using Servit.Infrastructure.Persistence;

namespace Servit.Api.Controllers.Admin;

// Read-only audit trail. The log itself is never mutated from the panel.
[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/audit-logs")]
public class AdminAuditLogsController(ServitDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AdminAuditLogDto>>> List([FromQuery] AdminAuditLogListQuery query)
    {
        var logs = dbContext.AdminAuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Action))
            logs = logs.Where(l => l.Action == query.Action);
        if (!string.IsNullOrWhiteSpace(query.EntityType))
            logs = logs.Where(l => l.EntityType == query.EntityType);
        if (query.AdminUserId is not null)
            logs = logs.Where(l => l.AdminUserId == query.AdminUserId);
        if (query.DateFrom is not null)
            logs = logs.Where(l => l.CreatedAt >= query.DateFrom);
        if (query.DateTo is not null)
            logs = logs.Where(l => l.CreatedAt <= query.DateTo);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            logs = logs.Where(l => EF.Functions.ILike(l.Action, $"%{term}%")
                || EF.Functions.ILike(l.EntityId, $"%{term}%"));
        }

        logs = query.Sort switch
        {
            "action" => logs.OrderByField(l => l.Action, query.Desc),
            _ => logs.OrderByField(l => l.CreatedAt, query.Desc || query.Sort is null),
        };

        var projected = logs.Select(l => new AdminAuditLogDto
        {
            Id = l.Id,
            AdminUserId = l.AdminUserId,
            AdminUserName = l.AdminUser != null ? l.AdminUser.FullName : null,
            Action = l.Action,
            EntityType = l.EntityType,
            EntityId = l.EntityId,
            MetadataJson = l.MetadataJson,
            OldValueJson = l.OldValueJson,
            NewValueJson = l.NewValueJson,
            CreatedAt = l.CreatedAt,
        });

        return Ok(await projected.ToPagedResponseAsync(query));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminAuditLogDto>> Get(Guid id)
    {
        var l = await dbContext.AdminAuditLogs
            .Where(x => x.Id == id)
            .Select(x => new AdminAuditLogDto
            {
                Id = x.Id,
                AdminUserId = x.AdminUserId,
                AdminUserName = x.AdminUser != null ? x.AdminUser.FullName : null,
                Action = x.Action,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                MetadataJson = x.MetadataJson,
                OldValueJson = x.OldValueJson,
                NewValueJson = x.NewValueJson,
                CreatedAt = x.CreatedAt,
            })
            .FirstOrDefaultAsync();

        return l is null ? NotFound() : Ok(l);
    }
}
