namespace ClinicPOS.Application.Common.Models;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public string? NextCursor { get; set; }
    public bool HasMore { get; set; }
    public int TotalCount { get; set; }

    public static PagedResult<T> Create(List<T> items, string? nextCursor, bool hasMore, int totalCount)
    {
        return new PagedResult<T>
        {
            Items = items,
            NextCursor = nextCursor,
            HasMore = hasMore,
            TotalCount = totalCount
        };
    }
}
