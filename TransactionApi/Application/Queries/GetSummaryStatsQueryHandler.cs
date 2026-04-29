using TransactionApi.Application.DTOs;
using TransactionApi.Application.Interfaces;

namespace TransactionApi.Application.Queries;

/// <summary>
/// Handles summary-statistics queries across all ingested transactions.
/// </summary>
public sealed class GetSummaryStatsQueryHandler
{
    private readonly ITransactionReadRepository _transactionRepository;

    /// <summary>
    /// Initializes the handler with the read repository used for aggregate queries.
    /// </summary>
    public GetSummaryStatsQueryHandler(ITransactionReadRepository transactionRepository)
        => _transactionRepository = transactionRepository;

    /// <summary>
    /// Returns aggregate transaction summary statistics.
    /// </summary>
    public Task<TransactionSummaryStats> HandleAsync(GetSummaryStatsQuery query, CancellationToken ct = default)
        => _transactionRepository.GetSummaryStatsAsync(query.IncludeCustomerBreakdowns, ct);
}
