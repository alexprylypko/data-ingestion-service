using Dapper;
using TransactionApi.Application.DTOs;
using TransactionApi.Application.Interfaces;
using TransactionApi.Domain.Models;

namespace TransactionApi.Infrastructure.Data;

/// <summary>
/// Executes query-side transaction reads using Dapper and PostgreSQL.
/// </summary>
public sealed class TransactionReadRepository : ITransactionReadRepository
{
    private readonly IReadDbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes the repository with the read-side connection factory.
    /// </summary>
    public TransactionReadRepository(IReadDbConnectionFactory connectionFactory)
        => _connectionFactory = connectionFactory;

    /// <inheritdoc />
    public async Task<(IEnumerable<Transaction> Items, int TotalCount)> GetByCustomerIdAsync(
        Guid customerId,
        int page = 1,
        int pageSize = 20,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        string? currency = null,
        string? sourceChannel = null,
        CancellationToken ct = default)
    {
        var (sql, parameters) = TransactionReadRepositorySqlBuilder.BuildCustomerTransactionsQuery(
            customerId,
            page,
            pageSize,
            fromDate,
            toDate,
            currency,
            sourceChannel);

        using var connection = _connectionFactory.CreateConnection();
        var rows = (await connection.QueryAsync<TransactionRow>(
            new CommandDefinition(sql, parameters, cancellationToken: ct))).ToList();

        return (
            rows.Select(static row => row.ToTransaction()),
            rows.FirstOrDefault()?.TotalCount ?? 0);
    }

    /// <inheritdoc />
    public async Task<TransactionSummaryStats> GetSummaryStatsAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                COUNT(*) AS TotalTransactions,
                COALESCE(SUM(amount), 0) AS TotalAmountUsd,
                COUNT(DISTINCT customer_id) AS UniqueCustomers,
                MIN(transaction_date) AS OldestTransaction,
                MAX(transaction_date) AS NewestTransaction
            FROM transactions;

            SELECT
                currency AS Currency,
                COUNT(*) AS Count,
                COALESCE(SUM(amount), 0) AS TotalAmount
            FROM transactions
            GROUP BY currency
            ORDER BY currency;

            SELECT
                source_channel AS Channel,
                COUNT(*) AS Count,
                COALESCE(SUM(amount), 0) AS TotalAmount
            FROM transactions
            GROUP BY source_channel
            ORDER BY source_channel;

            SELECT
                c.external_id AS CustomerId,
                t.currency AS Currency,
                COUNT(*) AS Count,
                COALESCE(SUM(t.amount), 0) AS TotalAmount
            FROM transactions t
            INNER JOIN customers c ON c.id = t.customer_id
            GROUP BY c.external_id, t.currency
            ORDER BY c.external_id, t.currency;

            SELECT
                c.external_id AS CustomerId,
                t.source_channel AS Channel,
                COUNT(*) AS Count,
                COALESCE(SUM(t.amount), 0) AS TotalAmount
            FROM transactions t
            INNER JOIN customers c ON c.id = t.customer_id
            GROUP BY c.external_id, t.source_channel
            ORDER BY c.external_id, t.source_channel;
            """;

        using var connection = _connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(sql, cancellationToken: ct));

        var summary = await multi.ReadSingleAsync<TransactionSummaryStats>();
        var byCurrency = (await multi.ReadAsync<CurrencyBreakdown>()).ToArray();
        var byChannel = (await multi.ReadAsync<ChannelBreakdown>()).ToArray();
        var byCustomerCurrency = (await multi.ReadAsync<CustomerCurrencyBreakdown>()).ToArray();
        var byCustomerChannel = (await multi.ReadAsync<CustomerChannelBreakdown>()).ToArray();

        summary.ByCurrency = byCurrency;
        summary.ByChannel = byChannel;
        summary.ByCustomerCurrency = byCustomerCurrency;
        summary.ByCustomerChannel = byCustomerChannel;
        return summary;
    }

    private sealed class TransactionRow
    {
        public Guid Id { get; init; }

        public Guid CustomerId { get; init; }

        public string ExternalTransactionId { get; init; } = string.Empty;

        public DateTimeOffset TransactionDate { get; init; }

        public decimal Amount { get; init; }

        public string Currency { get; init; } = string.Empty;

        public string SourceChannel { get; init; } = string.Empty;

        public DateTimeOffset CreatedAt { get; init; }

        public int TotalCount { get; init; }

        public Transaction ToTransaction() =>
            new()
            {
                Id = Id,
                CustomerId = CustomerId,
                ExternalTransactionId = ExternalTransactionId,
                TransactionDate = TransactionDate,
                Amount = Amount,
                Currency = Currency,
                SourceChannel = SourceChannel,
                CreatedAt = CreatedAt
            };
    }
}
