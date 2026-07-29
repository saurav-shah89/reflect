namespace Reflect.Models;

// One page of results plus the totals the pager needs.
public sealed class PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    public IReadOnlyList<T> Items { get; }

    // Across all pages, not just this one.
    public int TotalCount { get; }

    public int Page { get; }

    public int PageSize { get; }

    // Never 0, otherwise the pager has nothing to draw.
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    public static PagedResult<T> Empty(int pageSize) =>
        new(Array.Empty<T>(), totalCount: 0, page: 1, pageSize: pageSize);
}
