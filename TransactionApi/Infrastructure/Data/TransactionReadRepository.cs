using System.Text;
using Dapper;
using TransactionApi.Application.DTOs;
using TransactionApi.Application.Interfaces;
using TransactionApi.Domain.Models;

namespace TransactionApi.Infrastructure.Data;

/// <summary>Executes query-side transaction reads using Dapper and PostgreSQL.</summary>
public sealed class TransactionReadRepository : ITransactionReadRepository
{
    private readonly IReadDbConnectionFactory _connectionFactory;

    /// <summary>Initialises the repository with the read-side connection factory.</summary>
    public TransactionReadRepository(IReadDbConnectionFactory connectionFactory)
        => _connectionFactory = connectionFactory;

    /// <inheritdoc />
    public async Task<(IEnumerable<Transaction> Items, int TotalCount)> GetByCustomerIdAsync(
        Guid customerId,
        int page,
        int pageSize,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? currency,
        string? sourceChannel,
        CancellationToken ct = default)
    {
        var sql = new StringBuilder(
            """
            SELECT
                id AS Id,
                customer_id AS CustomerId,
                external_transaction_id AS ExternalTransactionId,
                transaction_date AS TransactionDate,
                amount AS Amount,
                currency AS Currency,
                source_channel AS SourceChannel,
                created_at AS CreatedAt,
                COUNT(*) OVER() AS TotalCount
            FROM transactions
            WHERE customer_id = @CustomerId
            """);

        var parameters = new DynamicParameters();
        parameters.Add("CustomerId", customerId);

        if (fromDate.HasValue)
        {
            sql.AppendLine("AND transaction_date >= @FromDate");
            parameters.Add("FromDate", fromDate.Value);
        }

        if (toDate.HasValue)
        {
            sql.AppendLine("AND transaction_date <= @ToDate");
            parameters.Add("ToDate", toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(currency))
        {
            sql.AppendLine("AND currency = @Currency");
            parameters.Add("Currency", currency);
        }

        if (!string.IsNullOrWhiteSpace(sourceChannel))
        {
            sql.AppendLine("AND source_channel = @SourceChannel");
            parameters.Add("SourceChannel", sourceChannel);
        }

        sql.AppendLine("ORDER BY transaction_date DESC");
        sql.AppendLine("LIMIT @PageSize OFFSET @Offset;");
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        using var connection = _connectionFactory.CreateConnection();
        var rows = (await connection.QueryAsync<TransactionRow>(
            new CommandDefinition(sql.ToString(), parameters, cancellationToken: ct))).ToList();

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
                COUNT(*) AS Count
            FROM transactions
            GROUP BY source_channel
            ORDER BY source_channel;
            """;

        using var connection = _connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(sql, cancellationToken: ct));

        var summary = await multi.ReadSingleAsync<TransactionSummaryStats>();
        var byCurrency = (await multi.ReadAsync<CurrencyBreakdown>()).ToArray();
        var byChannel = (await multi.ReadAsync<ChannelBreakdown>()).ToArray();

        summary.ByCurrency = byCurrency;
        summary.ByChannel = byChannel;
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
