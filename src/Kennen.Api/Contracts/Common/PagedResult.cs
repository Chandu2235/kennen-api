using System.ComponentModel.DataAnnotations;

namespace Kennen.Api.Contracts.Common;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class PagedQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>Capped at 100 so a caller cannot force the API to materialise an unbounded result set.</summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 25;
}
