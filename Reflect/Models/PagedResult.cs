namespace Reflect.Models;

/// <summary>
/// One page of results together with the totals a pager needs to render.
/// </summary>
/// <typeparam name="T">Element type of the page.</typeparam>
public sealed class PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    /// <summary>Items on the current page.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>Total items matching the query across all pages.</summary>
    public int TotalCount { get; }

    /// <summary>Current page number, 1-based.</summary>
    public int Page { get; }

    public int PageSize { get; }

    /// <summary>Total number of pages, minimum 1 so pagers always have a page to show.</summary>
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    /// <summary>An empty page, used when a query yields nothing.</summary>
    public static PagedResult<T> Empty(int pageSize) =>
        new(Array.Empty<T>(), totalCount: 0, page: 1, pageSize: pageSize);
}
