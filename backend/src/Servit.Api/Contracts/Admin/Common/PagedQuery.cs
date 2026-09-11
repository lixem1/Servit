namespace Servit.Api.Contracts.Admin.Common;

// Base query binding for admin list endpoints. Consumed by the React-Admin
// dataProvider (page/perPage/sort/order + resource-specific filters).
public class PagedQuery
{
    private const int MaxPerPage = 200;

    public int Page { get; set; } = 1;
    public int PerPage { get; set; } = 25;
    public string? Sort { get; set; }
    public string? Order { get; set; }

    public int NormalizedPerPage => PerPage < 1 ? 25 : Math.Min(PerPage, MaxPerPage);
    public int NormalizedPage => Page < 1 ? 1 : Page;
    public int Skip => (NormalizedPage - 1) * NormalizedPerPage;
    public bool Desc => string.Equals(Order, "DESC", StringComparison.OrdinalIgnoreCase);
}
