using Servit.Api.Contracts.Admin.Common;

namespace Servit.Api.Contracts.Admin.Reviews;

public class AdminReviewListQuery : PagedQuery
{
    public Guid? ProviderId { get; set; }
    public int? Rating { get; set; }
    public DateTimeOffset? DateFrom { get; set; }
    public DateTimeOffset? DateTo { get; set; }
    // Free-text over the comment.
    public string? Search { get; set; }
}
