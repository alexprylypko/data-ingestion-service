namespace TransactionApi.Application.DTOs;

/// <summary>Represents a paginated result set returned from a query endpoint.</summary>
/// <typeparam name="T">Type of item carried in the page.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>Items returned for the current page.</summary>
    public IReadOnlyCollection<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>Total number of items available across all pages.</summary>
    public int TotalCount { get; init; }

    /// <summary>Current one-based page number.</summary>
    public int Page { get; init; }

    /// <summary>Maximum number of items requested per page.</summary>
    public int PageSize { get; init; }

    /// <summary>Total number of pages based on the current page size.</summary>
    public int TotalPages { get; init; }
}
