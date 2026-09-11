namespace Servit.Api.Contracts.Admin.Common;

// { data, total } envelope the React-Admin dataProvider reads for lists.
public class PagedResponse<T>
{
    public required IReadOnlyList<T> Data { get; set; }
    public required int Total { get; set; }
}
