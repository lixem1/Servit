using Servit.Api.Contracts.Admin.Common;

namespace Servit.Api.Contracts.Admin.Categories;

public class AdminCategoryListQuery : PagedQuery
{
    // Free-text search over category name.
    public string? Search { get; set; }
}
