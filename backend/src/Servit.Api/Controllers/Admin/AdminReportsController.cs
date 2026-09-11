using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Servit.Api.Contracts.Admin.Reports;
using Servit.Domain.Constants;
using Servit.Domain.Enums;
using Servit.Infrastructure.Persistence;

namespace Servit.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/reports")]
public class AdminReportsController(ServitDbContext dbContext) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<SummaryReportDto>> Summary()
    {
        var since = DateTimeOffset.UtcNow.AddDays(-30);

        var totalUsers = await dbContext.Users.CountAsync();
        var newUsers = await dbContext.Users.CountAsync(u => u.CreatedAt >= since);
        var totalProviders = await dbContext.Providers.CountAsync();

        var pending = await dbContext.ServiceRequests.CountAsync(sr => sr.Status == ServiceRequestStatus.Pending);
        var assigned = await dbContext.ServiceRequests.CountAsync(sr => sr.Status == ServiceRequestStatus.Assigned);
        var completed = await dbContext.ServiceRequests.CountAsync(sr => sr.Status == ServiceRequestStatus.Completed);
        var cancelled = await dbContext.ServiceRequests.CountAsync(sr => sr.Status == ServiceRequestStatus.Cancelled);
        var total = pending + assigned + completed + cancelled;

        var gmv = await GmvAsync();

        return Ok(new SummaryReportDto
        {
            TotalUsers = totalUsers,
            NewUsersLast30Days = newUsers,
            TotalProviders = totalProviders,
            PendingRequests = pending,
            AssignedRequests = assigned,
            CompletedRequests = completed,
            CancelledRequests = cancelled,
            TotalRequests = total,
            CompletionRate = total == 0 ? 0 : (double)completed / total,
            Gmv = gmv,
        });
    }

    // Daily counts of new requests (metric=requests, default) or new users (metric=users).
    [HttpGet("timeseries")]
    public async Task<ActionResult<List<TimeseriesPointDto>>> Timeseries(
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, [FromQuery] string? metric)
    {
        var start = from ?? DateTimeOffset.UtcNow.AddDays(-30);
        var end = to ?? DateTimeOffset.UtcNow;

        List<DateTimeOffset> timestamps = string.Equals(metric, "users", StringComparison.OrdinalIgnoreCase)
            ? await dbContext.Users.Where(u => u.CreatedAt >= start && u.CreatedAt <= end)
                .Select(u => u.CreatedAt).ToListAsync()
            : await dbContext.ServiceRequests.Where(sr => sr.CreatedAt >= start && sr.CreatedAt <= end)
                .Select(sr => sr.CreatedAt).ToListAsync();

        var points = timestamps
            .GroupBy(ts => ts.UtcDateTime.Date)
            .Select(g => new TimeseriesPointDto { Date = g.Key, Count = g.Count() })
            .OrderBy(p => p.Date)
            .ToList();

        return Ok(points);
    }

    // dimension=providers (by GMV) | categories (by request count).
    [HttpGet("top")]
    public async Task<ActionResult<List<TopItemDto>>> Top([FromQuery] string dimension = "providers", [FromQuery] int limit = 10)
    {
        limit = Math.Clamp(limit, 1, 50);

        if (string.Equals(dimension, "categories", StringComparison.OrdinalIgnoreCase))
        {
            var categories = await dbContext.Categories
                .Select(c => new TopItemDto
                {
                    Id = c.Id.ToString(),
                    Name = c.Name,
                    Value = dbContext.ServiceRequests.Count(sr => sr.CategoryId == c.Id),
                })
                .OrderByDescending(x => x.Value)
                .Take(limit)
                .ToListAsync();
            return Ok(categories);
        }

        var providers = await dbContext.Providers
            .Select(p => new TopItemDto
            {
                Id = p.Id.ToString(),
                Name = p.User.FullName,
                Value = dbContext.ProviderResponses
                    .Where(r => r.ProviderId == p.Id &&
                        r.Status == ProviderResponseStatus.Accepted &&
                        r.ServiceRequest.Status == ServiceRequestStatus.Completed)
                    .Sum(r => (decimal?)r.ProposedPrice) ?? 0m,
            })
            .OrderByDescending(x => x.Value)
            .Take(limit)
            .ToListAsync();
        return Ok(providers);
    }

    // Downloadable CSV of service requests, optionally date-filtered.
    [HttpGet("export.csv")]
    public async Task<IActionResult> ExportCsv([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to)
    {
        var query = dbContext.ServiceRequests.AsQueryable();
        if (from is not null) query = query.Where(sr => sr.CreatedAt >= from);
        if (to is not null) query = query.Where(sr => sr.CreatedAt <= to);

        var rows = await query
            .OrderByDescending(sr => sr.CreatedAt)
            .Select(sr => new
            {
                sr.Id,
                sr.CreatedAt,
                Status = sr.Status.ToString(),
                Category = sr.Category.Name,
                Customer = sr.Customer.FullName,
                AcceptedPrice = sr.Responses
                    .Where(r => r.Status == ProviderResponseStatus.Accepted)
                    .Select(r => r.ProposedPrice)
                    .FirstOrDefault(),
            })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Id,CreatedAt,Status,Category,Customer,AcceptedPrice");
        foreach (var r in rows)
        {
            sb.Append(r.Id).Append(',')
              .Append(r.CreatedAt.UtcDateTime.ToString("o", CultureInfo.InvariantCulture)).Append(',')
              .Append(Csv(r.Status)).Append(',')
              .Append(Csv(r.Category)).Append(',')
              .Append(Csv(r.Customer)).Append(',')
              .Append(r.AcceptedPrice?.ToString(CultureInfo.InvariantCulture) ?? "")
              .Append('\n');
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "service-requests.csv");
    }

    private async Task<decimal> GmvAsync() =>
        await dbContext.ProviderResponses
            .Where(r => r.Status == ProviderResponseStatus.Accepted &&
                r.ServiceRequest.Status == ServiceRequestStatus.Completed)
            .SumAsync(r => (decimal?)r.ProposedPrice) ?? 0m;

    // Minimal CSV field escaping: quote when the value contains a comma, quote or newline.
    private static string Csv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }
}
