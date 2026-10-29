using Microsoft.EntityFrameworkCore;

namespace Smart_AIUB_Service_Issue.Common;

/// <summary>Paging info without the item type, so one _Pagination partial works for every list.</summary>
public interface IPagedResult
{
    int Page { get; }
    int PageSize { get; }
    int TotalCount { get; }
    int TotalPages { get; }
    bool HasPrevious { get; }
    bool HasNext { get; }
}

/// <summary>One page of a list plus the numbers needed to draw page links.</summary>
public class PagedResult<T> : IPagedResult
{
    public PagedResult(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public static class PagingExtensions
{
    public const int DefaultPageSize = 10;

    /// <summary>
    /// Runs two SQL queries: COUNT(*) for the total, then only the rows of the requested page
    /// (OFFSET/FETCH). The query must already be ordered. Out-of-range pages are clamped.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, int page, int pageSize = DefaultPageSize)
    {
        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<T>(items, page, pageSize, totalCount);
    }
}
