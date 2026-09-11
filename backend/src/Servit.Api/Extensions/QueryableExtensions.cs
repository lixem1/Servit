using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Servit.Api.Contracts.Admin.Common;

namespace Servit.Api.Extensions;

public static class QueryableExtensions
{
    // Counts the full (filtered) set, then returns just the requested page.
    public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(
        this IQueryable<T> query, PagedQuery paging, CancellationToken cancellationToken = default)
    {
        var total = await query.CountAsync(cancellationToken);
        var data = await query
            .Skip(paging.Skip)
            .Take(paging.NormalizedPerPage)
            .ToListAsync(cancellationToken);
        return new PagedResponse<T> { Data = data, Total = total };
    }

    public static IQueryable<T> OrderByField<T, TKey>(
        this IQueryable<T> query, Expression<Func<T, TKey>> keySelector, bool desc)
        => desc ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
}
