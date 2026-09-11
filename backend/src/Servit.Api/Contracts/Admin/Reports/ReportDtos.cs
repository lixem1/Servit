namespace Servit.Api.Contracts.Admin.Reports;

public class SummaryReportDto
{
    public required int TotalUsers { get; set; }
    public required int NewUsersLast30Days { get; set; }
    public required int TotalProviders { get; set; }

    public required int PendingRequests { get; set; }
    public required int AssignedRequests { get; set; }
    public required int CompletedRequests { get; set; }
    public required int CancelledRequests { get; set; }
    public required int TotalRequests { get; set; }

    // completed / total, 0..1.
    public required double CompletionRate { get; set; }
    // Sum of accepted ProposedPrice on completed requests.
    public required decimal Gmv { get; set; }
}

public class TimeseriesPointDto
{
    public required DateTime Date { get; set; }
    public required int Count { get; set; }
}

public class TopItemDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required decimal Value { get; set; }
}
