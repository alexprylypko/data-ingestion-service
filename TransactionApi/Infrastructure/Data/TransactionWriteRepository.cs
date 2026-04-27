using Dapper;
using TransactionApi.Application.Interfaces;
using TransactionApi.Domain.Models;

namespace TransactionApi.Infrastructure.Data;

/// <summary>Persists transaction command-side data using Dapper and PostgreSQL.</summary>
public sealed class TransactionWriteRepository : ITransactionWriteRepository
{
    private readonly IWriteDbConnectionFactory _connectionFactory;

    /// <summary>Initialises the repository with the write-side connection factory.</summary>
    public TransactionWriteRepository(IWriteDbConnectionFactory connectionFactory)
        => _connectionFactory = connectionFactory;

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string externalTransactionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 1
            FROM transactions
            WHERE external_transaction_id = @Id
            LIMIT 1;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(sql, new { Id = externalTransactionId }, cancellationToken: ct));

        return result.HasValue;
    }

    /// <inheritdoc />
    public async Task InsertAsync(Transaction transaction, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO transactions (
                id,
                customer_id,
                external_transaction_id,
                transaction_date,
                amount,
                currency,
                source_channel,
                created_at)
            VALUES (
                @Id,
                @CustomerId,
                @ExternalTransactionId,
                @TransactionDate,
                @Amount,
                @Currency,
                @SourceChannel,
                @CreatedAt);
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, transaction, cancellationToken: ct));
    }
}
