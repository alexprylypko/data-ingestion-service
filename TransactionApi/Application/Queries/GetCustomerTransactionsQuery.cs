namespace TransactionApi.Application.Queries;

/// <summary>
/// Represents a request for a paginated transaction list for a single customer.
/// </summary>
public sealed class GetCustomerTransactionsQuery
{
    /// <summary>
    /// External customer identifier supplied by the API caller.
    /// </summary>
    public string CustomerId { get; init; } = string.Empty;

    /// <summary>
    /// Requested one-based page number.
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Requested maximum number of items per page.
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Optional inclusive lower bound for transaction date filtering.
    /// </summary>
    public DateTimeOffset? FromDate { get; init; }

    /// <summary>
    /// Optional inclusive upper bound for transaction date filtering.
    /// </summary>
    public DateTimeOffset? ToDate { get; init; }

    /// <summary>
    /// Optional currency filter.
    /// </summary>
    public string? Currency { get; init; }

    /// <summary>
    /// Optional source-channel filter.
    /// </summary>
    public string? SourceChannel { get; init; }
}
