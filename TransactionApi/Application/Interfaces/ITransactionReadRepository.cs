using TransactionApi.Application.DTOs;
using TransactionApi.Domain.Models;

namespace TransactionApi.Application.Interfaces;

/// <summary>
/// Read-only query operations for the <see cref="Transaction"/> aggregate.
/// </summary>
public interface ITransactionReadRepository
{
    /// <summary>
    /// Returns a paginated, filtered list of transactions for the specified customer.
    /// </summary>
    Task<(IEnumerable<Transaction> Items, int TotalCount)> GetByCustomerIdAsync(
        Guid customerId,
        int page = 1,
        int pageSize = 20,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        string? currency = null,
        string? sourceChannel = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns aggregate statistics across all ingested transactions.
    /// </summary>
    Task<TransactionSummaryStats> GetSummaryStatsAsync(CancellationToken ct = default);
}
