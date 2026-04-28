using Dapper;
using TransactionApi.Application.Interfaces;
using TransactionApi.Domain.Models;

namespace TransactionApi.Infrastructure.Data;

/// <summary>
/// Provides customer persistence using the primary PostgreSQL endpoint.
/// </summary>
public sealed class CustomerRepository : ICustomerRepository
{
    private readonly IWriteDbConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes the repository with the write-side connection factory.
    /// </summary>
    public CustomerRepository(IWriteDbConnectionFactory connectionFactory)
        => _connectionFactory = connectionFactory;

    /// <inheritdoc />
    public async Task<Customer?> GetByExternalIdAsync(string externalId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                id,
                external_id AS ExternalId,
                created_at AS CreatedAt
            FROM customers
            WHERE external_id = @ExternalId
            LIMIT 1;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Customer>(
            new CommandDefinition(sql, new { ExternalId = externalId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Performs an INSERT … ON CONFLICT DO UPDATE RETURNING in one round-trip,
    /// replacing the old two-query pattern (INSERT then SELECT).
    /// </remarks>
    public async Task<Customer> GetOrCreateAsync(string externalId, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO customers (id, external_id, created_at)
            VALUES (@Id, @ExternalId, @CreatedAt)
            ON CONFLICT (external_id) DO UPDATE
                SET external_id = EXCLUDED.external_id
            RETURNING id, external_id AS ExternalId, created_at AS CreatedAt;
            """;

        var candidate = new Customer
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<Customer>(
            new CommandDefinition(sql, candidate, cancellationToken: ct));
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Uses <c>UNNEST</c> to feed a set of candidate rows into a single
    /// INSERT … ON CONFLICT … RETURNING statement, then fetches all the
    /// resulting rows in one query.
    /// </para>
    /// <para>
    /// This replaces up to N individual <see cref="GetOrCreateAsync"/> calls
    /// (one per distinct customer in a batch) with exactly two database
    /// round-trips: one UNNEST insert and one SELECT.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyDictionary<string, Customer>> BulkGetOrCreateAsync(
        IEnumerable<string> externalIds,
        CancellationToken ct = default)
    {
        var ids = externalIds as string[] ?? externalIds.ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<string, Customer>(StringComparer.Ordinal);
        }

        // Generate candidate GUIDs and timestamps upfront so the UNNEST values
        // are deterministic and can be sent as typed arrays to PostgreSQL.
        var newIds = ids.Select(_ => Guid.NewGuid()).ToArray();
        var now = DateTimeOffset.UtcNow;
        var timestamps = Enumerable.Repeat(now, ids.Length).ToArray();

        // One INSERT for all new customers, skip conflicts, return everything
        // (existing + newly inserted) in a single SELECT.
        const string upsertSql = """
            INSERT INTO customers (id, external_id, created_at)
            SELECT * FROM UNNEST(@Ids::uuid[], @ExternalIds::text[], @CreatedAts::timestamptz[])
                AS t(id, external_id, created_at)
            ON CONFLICT (external_id) DO NOTHING;
            """;

        const string selectSql = """
            SELECT
                id,
                external_id AS ExternalId,
                created_at AS CreatedAt
            FROM customers
            WHERE external_id = ANY(@ExternalIds);
            """;

        using var connection = _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(new CommandDefinition(
            upsertSql,
            new { Ids = newIds, ExternalIds = ids, CreatedAts = timestamps },
            cancellationToken: ct));

        var customers = await connection.QueryAsync<Customer>(new CommandDefinition(
            selectSql,
            new { ExternalIds = ids },
            cancellationToken: ct));

        return customers.ToDictionary(
            static c => c.ExternalId,
            StringComparer.Ordinal);
    }
}
