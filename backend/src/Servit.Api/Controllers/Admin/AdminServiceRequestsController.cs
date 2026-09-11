using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Servit.Api.Contracts.Admin.Common;
using Servit.Api.Contracts.Admin.ServiceRequests;
using Servit.Api.Extensions;
using Servit.Api.Services;
using Servit.Domain.Constants;
using Servit.Domain.Entities;
using Servit.Domain.Enums;
using Servit.Infrastructure.Persistence;

namespace Servit.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/service-requests")]
public class AdminServiceRequestsController(
    ServitDbContext dbContext,
    IFileStorageService fileStorage,
    IAdminAuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AdminServiceRequestDto>>> List(
        [FromQuery] AdminServiceRequestListQuery query)
    {
        var requests = dbContext.ServiceRequests.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            requests = requests.Where(sr =>
                EF.Functions.ILike(sr.Description, $"%{term}%") ||
                EF.Functions.ILike(sr.Customer.FullName, $"%{term}%") ||
                EF.Functions.ILike(sr.Category.Name, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<ServiceRequestStatus>(query.Status, ignoreCase: true, out var status))
        {
            requests = requests.Where(sr => sr.Status == status);
        }

        if (query.CategoryId is not null) requests = requests.Where(sr => sr.CategoryId == query.CategoryId);
        if (query.CustomerId is not null) requests = requests.Where(sr => sr.CustomerId == query.CustomerId);
        if (query.ProviderId is not null)
            requests = requests.Where(sr => sr.Responses.Any(r => r.ProviderId == query.ProviderId));
        if (query.DateFrom is not null) requests = requests.Where(sr => sr.CreatedAt >= query.DateFrom);
        if (query.DateTo is not null) requests = requests.Where(sr => sr.CreatedAt <= query.DateTo);

        requests = query.Sort switch
        {
            "status" => requests.OrderByField(sr => sr.Status, query.Desc),
            "categoryName" => requests.OrderByField(sr => sr.Category.Name, query.Desc),
            _ => requests.OrderByField(sr => sr.CreatedAt, query.Desc || query.Sort is null),
        };

        var projected = requests.Select(sr => new AdminServiceRequestDto
        {
            Id = sr.Id,
            CategoryId = sr.CategoryId,
            CategoryName = sr.Category.Name,
            CustomerId = sr.CustomerId,
            CustomerName = sr.Customer.FullName,
            Description = sr.Description,
            Status = sr.Status.ToString(),
            CreatedAt = sr.CreatedAt,
            ResponseCount = sr.Responses.Count,
            AcceptedProviderName = sr.Responses
                .Where(r => r.Status == ProviderResponseStatus.Accepted)
                .Select(r => r.Provider.User.FullName)
                .FirstOrDefault(),
        });

        return Ok(await projected.ToPagedResponseAsync(query));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminServiceRequestDetailDto>> GetOne(Guid id)
    {
        var sr = await dbContext.ServiceRequests
            .Include(x => x.Category)
            .Include(x => x.Customer)
            .Include(x => x.Attachments)
            .Include(x => x.Responses).ThenInclude(r => r.Provider).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (sr is null) return NotFound();

        var review = await dbContext.Reviews.FirstOrDefaultAsync(r => r.ServiceRequestId == id);

        var quotes = sr.Responses
            .OrderBy(r => r.CreatedAt)
            .Select(r => new AdminQuoteDto
            {
                Id = r.Id,
                ProviderId = r.ProviderId,
                ProviderName = r.Provider.User.FullName,
                Message = r.Message,
                ProposedPrice = r.ProposedPrice,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt,
            })
            .ToList();

        return Ok(new AdminServiceRequestDetailDto
        {
            Id = sr.Id,
            CategoryId = sr.CategoryId,
            CategoryName = sr.Category.Name,
            CustomerId = sr.CustomerId,
            CustomerName = sr.Customer.FullName,
            CustomerEmail = sr.Customer.Email!,
            Description = sr.Description,
            Lat = sr.Location.Y,
            Lng = sr.Location.X,
            Status = sr.Status.ToString(),
            CreatedAt = sr.CreatedAt,
            Attachments = sr.Attachments.Select(a => new AdminAttachmentDto
            {
                Id = a.Id,
                Type = a.Type.ToString(),
                FileName = a.FileName,
            }).ToList(),
            Quotes = quotes,
            Timeline = BuildTimeline(sr, quotes, review),
            Review = review is null ? null : new AdminRequestReviewDto
            {
                Id = review.Id,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
            },
        });
    }

    // Serves attachment bytes for the admin panel. The customer/provider endpoint
    // forbids Admin, so admins get their own read-only access here.
    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> GetAttachment(Guid id, Guid attachmentId)
    {
        var attachment = await dbContext.ServiceRequestAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.ServiceRequestId == id);
        if (attachment is null) return NotFound();

        var stream = fileStorage.OpenRead(attachment.StoragePath);
        return File(stream, attachment.ContentType, attachment.FileName);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateRequestStatusRequest request)
    {
        if (!Enum.TryParse<ServiceRequestStatus>(request.Status, ignoreCase: true, out var newStatus))
        {
            return BadRequest($"Estado inválido. Debe ser uno de: {string.Join(", ", Enum.GetNames<ServiceRequestStatus>())}.");
        }

        var sr = await dbContext.ServiceRequests.FindAsync(id);
        if (sr is null) return NotFound();

        var old = sr.Status;
        if (old == newStatus) return NoContent();

        sr.Status = newStatus;
        await dbContext.SaveChangesAsync();

        await audit.LogAsync(User.GetUserId(), "service-request.status", nameof(ServiceRequest), id.ToString(),
            oldValue: new { Status = old.ToString() }, newValue: new { Status = newStatus.ToString() });

        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var sr = await dbContext.ServiceRequests.FindAsync(id);
        if (sr is null) return NotFound();
        if (sr.Status is ServiceRequestStatus.Completed or ServiceRequestStatus.Cancelled)
        {
            return BadRequest("La solicitud ya está en un estado terminal.");
        }

        var old = sr.Status;
        sr.Status = ServiceRequestStatus.Cancelled;
        await dbContext.SaveChangesAsync();

        await audit.LogAsync(User.GetUserId(), "service-request.cancel", nameof(ServiceRequest), id.ToString(),
            oldValue: new { Status = old.ToString() });

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var sr = await dbContext.ServiceRequests.FindAsync(id);
        if (sr is null) return NotFound();

        // Responses, attachments and reviews cascade-delete via FK config.
        // Note: attachment files on disk are not removed (storage has no delete API).
        dbContext.ServiceRequests.Remove(sr);
        await dbContext.SaveChangesAsync();

        await audit.LogAsync(User.GetUserId(), "service-request.delete", nameof(ServiceRequest), id.ToString(),
            oldValue: new { sr.Description, Status = sr.Status.ToString() });

        return NoContent();
    }

    private static List<AdminTimelineEventDto> BuildTimeline(
        ServiceRequest sr, List<AdminQuoteDto> quotes, Review? review)
    {
        var events = new List<AdminTimelineEventDto>
        {
            new() { Type = "created", At = sr.CreatedAt, Description = "Solicitud creada" },
        };

        events.AddRange(quotes.Select(q => new AdminTimelineEventDto
        {
            Type = "quote",
            At = q.CreatedAt,
            Description = $"Cotización de {q.ProviderName}" +
                (q.ProposedPrice is not null ? $" — {q.ProposedPrice:0.##}" : ""),
        }));

        var accepted = quotes.FirstOrDefault(q => q.Status == nameof(ProviderResponseStatus.Accepted));
        if (accepted is not null)
        {
            events.Add(new AdminTimelineEventDto
            {
                Type = "assigned",
                At = accepted.CreatedAt,
                Description = $"Asignada a {accepted.ProviderName}",
            });
        }

        if (sr.Status == ServiceRequestStatus.Completed)
        {
            events.Add(new AdminTimelineEventDto
            {
                Type = "completed",
                At = review?.CreatedAt,
                Description = "Marcada como completada",
            });
        }
        else if (sr.Status == ServiceRequestStatus.Cancelled)
        {
            events.Add(new AdminTimelineEventDto
            {
                Type = "cancelled",
                At = null,
                Description = "Cancelada",
            });
        }

        return events.OrderBy(e => e.At ?? DateTimeOffset.MaxValue).ToList();
    }
}
