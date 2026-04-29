namespace TransactionApi.Application.Queries;

/// <summary>
/// Represents a request for aggregate transaction summary statistics.
/// </summary>
public sealed class GetSummaryStatsQuery
{
    /// <summary>
    /// Gets or sets a value indicating whether customer-level breakdowns should be included.
    /// </summary>
    public bool IncludeCustomerBreakdowns { get; init; }
}
