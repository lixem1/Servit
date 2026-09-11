namespace Servit.Api.Contracts.Admin.Analytics;

public class FunnelDto
{
    public required int Requests { get; set; }
    public required int WithQuotes { get; set; }
    public required int Assigned { get; set; }
    public required int Completed { get; set; }
}

public class ResponseTimesDto
{
    // Average seconds from request creation to first provider quote.
    public required double? AverageFirstResponseSeconds { get; set; }
    public required int RequestsWithResponse { get; set; }
    // Average seconds from request creation to the accepted quote (time in queue).
    public required double? AverageTimeInQueueSeconds { get; set; }
    public required int AssignedRequestsMeasured { get; set; }
}

public class StaleRequestDto
{
    public required Guid Id { get; set; }
    public required string Description { get; set; }
    public required string CategoryName { get; set; }
    public required string CustomerName { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required double AgeHours { get; set; }
}
