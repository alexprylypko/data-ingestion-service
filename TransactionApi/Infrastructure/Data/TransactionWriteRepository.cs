using Dapper;
using Npgsql;
using TransactionApi.Application.Interfaces;
using TransactionApi.Domain.Exceptions;
using TransactionApi.Domain.Models;

namespace TransactionApi.Infrastructure.Data;

/// <summary>
/// Persists transaction command-side data using Dapper and PostgreSQL.
/// </summary>
public sealed class TransactionWriteRepository : ITransactionWriteRepository
{
    private readonly IWriteDbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes the repository with the write-side connection factory.
    /// </summary>
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
        try
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, transaction, cancellationToken: ct));
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DuplicateTransactionException(
                $"Transaction '{transaction.ExternalTransactionId}' already exists.",
                exception);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uses a single <c>= ANY(@Ids)</c> clause to check all IDs in one round-trip.
    /// PostgreSQL's <c>ANY</c> operator handles arrays efficiently with the index on
    /// <c>external_transaction_id</c>.
    /// </remarks>
    public async Task<IReadOnlySet<string>> GetExistingIdsAsync(
        IEnumerable<string> externalIds,
        CancellationToken ct = default)
    {
        var ids = externalIds as string[] ?? externalIds.ToArray();

        if (ids.Length == 0)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        const string sql = """
            SELECT external_transaction_id
            FROM transactions
            WHERE external_transaction_id = ANY(@Ids);
            """;

        using var connection = _connectionFactory.CreateConnection();
        var existing = await connection.QueryAsync<string>(
            new CommandDefinition(sql, new { Ids = ids }, cancellationToken: ct));

        return new HashSet<string>(existing, StringComparer.Ordinal);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Uses <see cref="NpgsqlConnection.BeginBinaryImport"/> (PostgreSQL COPY protocol)
    /// when the collection is large enough to justify it, and falls back to a parameterised
    /// multi-row <c>INSERT … VALUES</c> statement for small collections.
    /// </para>
    /// <para>
    /// The <c>ON CONFLICT (external_transaction_id) DO NOTHING</c> clause makes the insert
    /// idempotent — rows that sneak in between the duplicate check and this call are silently
    /// skipped rather than causing an exception.
    /// </para>
    /// <para>
    /// <strong>COPY threshold:</strong> COPY carries a fixed per-statement overhead that is
    /// only worth paying for batches larger than ~50 rows.  Below that threshold the
    /// parameterised INSERT is faster.
    /// </para>
    /// </remarks>
    public async Task BulkInsertAsync(
        IReadOnlyCollection<Transaction> transactions,
        CancellationToken ct = default)
    {
        if (transactions.Count == 0)
        {
            return;
        }

        if (transactions.Count <= 50)
        {
            await BulkInsertWithValuesAsync(transactions, ct);
        }
        else
        {
            await BulkInsertWithCopyAsync(transactions, ct);
        }
    }

    // -------------------------------------------------------------------------
    // BulkInsert strategies
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds a single multi-row INSERT for small batches.
    /// Dapper does not natively support multi-row VALUE lists, so the SQL
    /// is built manually with per-row numbered parameters.
    /// </summary>
    private async Task BulkInsertWithValuesAsync(
        IReadOnlyCollection<Transaction> transactions,
        CancellationToken ct)
    {
        var valuesClauses = new List<string>(transactions.Count);
        var parameters = new DynamicParameters();
        var i = 0;

        foreach (var t in transactions)
        {
            valuesClauses.Add(
                $"(@Id{i}, @CustomerId{i}, @ExternalTransactionId{i}, " +
                $"@TransactionDate{i}, @Amount{i}, @Currency{i}, " +
                $"@SourceChannel{i}, @CreatedAt{i})");

            parameters.Add($"Id{i}", t.Id);
            parameters.Add($"CustomerId{i}", t.CustomerId);
            parameters.Add($"ExternalTransactionId{i}", t.ExternalTransactionId);
            parameters.Add($"TransactionDate{i}", t.TransactionDate);
            parameters.Add($"Amount{i}", t.Amount);
            parameters.Add($"Currency{i}", t.Currency);
            parameters.Add($"SourceChannel{i}", t.SourceChannel);
            parameters.Add($"CreatedAt{i}", t.CreatedAt);
            i++;
        }

        var sql =
            $"""
            INSERT INTO transactions (
                id, customer_id, external_transaction_id,
                transaction_date, amount, currency,
                source_channel, created_at)
            VALUES {string.Join(", ", valuesClauses)}
            ON CONFLICT (external_transaction_id) DO NOTHING;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: ct));
    }

    /// <summary>
    /// Uses the PostgreSQL binary COPY protocol for large batches.
    /// COPY bypasses the query planner and is significantly faster than
    /// multi-row INSERT for hundreds of rows.
    /// </summary>
    private async Task BulkInsertWithCopyAsync(
        IReadOnlyCollection<Transaction> transactions,
        CancellationToken ct)
    {
        // NpgsqlConnection is required for COPY — cast from IDbConnection
        using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        await using var writer = await connection.BeginBinaryImportAsync(
            """
            COPY transactions (
                id, customer_id, external_transaction_id,
                transaction_date, amount, currency,
                source_channel, created_at)
            FROM STDIN (FORMAT BINARY)
            """,
            ct);

        foreach (var t in transactions)
        {
            await writer.StartRowAsync(ct);
            await writer.WriteAsync(t.Id, NpgsqlTypes.NpgsqlDbType.Uuid, ct);
            await writer.WriteAsync(t.CustomerId, NpgsqlTypes.NpgsqlDbType.Uuid, ct);
            await writer.WriteAsync(t.ExternalTransactionId, NpgsqlTypes.NpgsqlDbType.Text, ct);
            await writer.WriteAsync(t.TransactionDate, NpgsqlTypes.NpgsqlDbType.TimestampTz, ct);
            await writer.WriteAsync(t.Amount, NpgsqlTypes.NpgsqlDbType.Numeric, ct);
            await writer.WriteAsync(t.Currency, NpgsqlTypes.NpgsqlDbType.Text, ct);
            await writer.WriteAsync(t.SourceChannel, NpgsqlTypes.NpgsqlDbType.Text, ct);
            await writer.WriteAsync(t.CreatedAt, NpgsqlTypes.NpgsqlDbType.TimestampTz, ct);
        }

        // Completes the COPY stream and flushes to PostgreSQL
        await writer.CompleteAsync(ct);
    }
}
