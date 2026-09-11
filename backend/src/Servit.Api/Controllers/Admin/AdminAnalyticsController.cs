using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Servit.Api.Contracts.Admin.Analytics;
using Servit.Domain.Constants;
using Servit.Domain.Enums;
using Servit.Infrastructure.Persistence;

namespace Servit.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/analytics")]
public class AdminAnalyticsController(ServitDbContext dbContext) : ControllerBase
{
    // Conversion funnel over an optional date range.
    [HttpGet("funnel")]
    public async Task<ActionResult<FunnelDto>> Funnel([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to)
    {
        var query = dbContext.ServiceRequests.AsQueryable();
        if (from is not null) query = query.Where(sr => sr.CreatedAt >= from);
        if (to is not null) query = query.Where(sr => sr.CreatedAt <= to);

        var requests = await query.CountAsync();
        var withQuotes = await query.CountAsync(sr => sr.Responses.Any());
        // Assigned or beyond: a completed request was necessarily assigned first.
        var assigned = await query.CountAsync(sr =>
            sr.Status == ServiceRequestStatus.Assigned || sr.Status == ServiceRequestStatus.Completed);
        var completed = await query.CountAsync(sr => sr.Status == ServiceRequestStatus.Completed);

        return Ok(new FunnelDto
        {
            Requests = requests,
            WithQuotes = withQuotes,
            Assigned = assigned,
            Completed = completed,
        });
    }

    // First-response SLA and time-in-queue averages.
    [HttpGet("response-times")]
    public async Task<ActionResult<ResponseTimesDto>> ResponseTimes([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to)
    {
        var query = dbContext.ServiceRequests.AsQueryable();
        if (from is not null) query = query.Where(sr => sr.CreatedAt >= from);
        if (to is not null) query = query.Where(sr => sr.CreatedAt <= to);

        var firstResponse = await query
            .Where(sr => sr.Responses.Any())
            .Select(sr => new
            {
                sr.CreatedAt,
                First = sr.Responses.Min(r => (DateTimeOffset?)r.CreatedAt),
            })
            .ToListAsync();

        double? avgFirst = firstResponse.Count == 0
            ? null
            : firstResponse.Average(x => (x.First!.Value - x.CreatedAt).TotalSeconds);

        var accepted = await query
            .Where(sr => sr.Responses.Any(r => r.Status == ProviderResponseStatus.Accepted))
            .Select(sr => new
            {
                sr.CreatedAt,
                Accepted = sr.Responses
                    .Where(r => r.Status == ProviderResponseStatus.Accepted)
                    .Min(r => (DateTimeOffset?)r.CreatedAt),
            })
            .ToListAsync();

        double? avgQueue = accepted.Count == 0
            ? null
            : accepted.Average(x => (x.Accepted!.Value - x.CreatedAt).TotalSeconds);

        return Ok(new ResponseTimesDto
        {
            AverageFirstResponseSeconds = avgFirst,
            RequestsWithResponse = firstResponse.Count,
            AverageTimeInQueueSeconds = avgQueue,
            AssignedRequestsMeasured = accepted.Count,
        });
    }

    // Pending requests with zero quotes older than `hours` (default 24).
    [HttpGet("stale")]
    public async Task<ActionResult<List<StaleRequestDto>>> Stale([FromQuery] int hours = 24)
    {
        hours = Math.Max(1, hours);
        var cutoff = DateTimeOffset.UtcNow.AddHours(-hours);

        var rows = await dbContext.ServiceRequests
            .Where(sr => sr.Status == ServiceRequestStatus.Pending
                && sr.CreatedAt <= cutoff
                && !sr.Responses.Any())
            .OrderBy(sr => sr.CreatedAt)
            .Select(sr => new
            {
                sr.Id,
                sr.Description,
                CategoryName = sr.Category.Name,
                CustomerName = sr.Customer.FullName,
                sr.CreatedAt,
            })
            .ToListAsync();

        var now = DateTimeOffset.UtcNow;
        var result = rows.Select(r => new StaleRequestDto
        {
            Id = r.Id,
            Description = r.Description,
            CategoryName = r.CategoryName,
            CustomerName = r.CustomerName,
            CreatedAt = r.CreatedAt,
            AgeHours = Math.Round((now - r.CreatedAt).TotalHours, 1),
        }).ToList();

        return Ok(result);
    }
}
