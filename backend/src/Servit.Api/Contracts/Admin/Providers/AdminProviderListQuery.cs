using Servit.Api.Contracts.Admin.Common;

namespace Servit.Api.Contracts.Admin.Providers;

public class AdminProviderListQuery : PagedQuery
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
}
