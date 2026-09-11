using Servit.Api.Contracts.Admin.Common;

namespace Servit.Api.Contracts.Admin.Users;

public class AdminUserListQuery : PagedQuery
{
    // Free-text over full name or email.
    public string? Search { get; set; }
    // Filter by role name (Customer|Provider|Admin).
    public string? Role { get; set; }
    // "active" | "suspended".
    public string? Status { get; set; }
    public DateTimeOffset? CreatedFrom { get; set; }
    public DateTimeOffset? CreatedTo { get; set; }
}
