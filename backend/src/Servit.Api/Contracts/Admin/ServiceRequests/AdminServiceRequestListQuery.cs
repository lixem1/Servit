using Servit.Api.Contracts.Admin.Common;

namespace Servit.Api.Contracts.Admin.ServiceRequests;

public class AdminServiceRequestListQuery : PagedQuery
{
    // Global search over description, customer name and category name.
    public string? Search { get; set; }
    // Segment: Pending | Assigned | Completed | Cancelled.
    public string? Status { get; set; }
    public int? CategoryId { get; set; }
    public Guid? CustomerId { get; set; }
    // Requests where this provider submitted a response.
    public Guid? ProviderId { get; set; }
    public DateTimeOffset? DateFrom { get; set; }
    public DateTimeOffset? DateTo { get; set; }
}
