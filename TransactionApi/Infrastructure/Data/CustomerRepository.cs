using Dapper;
using TransactionApi.Application.Interfaces;
using TransactionApi.Domain.Models;

namespace TransactionApi.Infrastructure.Data;

/// <summary>Provides customer persistence using the primary PostgreSQL endpoint.</summary>
public sealed class CustomerRepository : ICustomerRepository
{
    private readonly IWriteDbConnectionFactory _connectionFactory;

    /// <summary>Initialises the repository with the write-side connection factory.</summary>
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
    public async Task<Customer> GetOrCreateAsync(string externalId, CancellationToken ct = default)
    {
        const string insertSql = """
            INSERT INTO customers (id, external_id, created_at)
            VALUES (@Id, @ExternalId, @CreatedAt)
            ON CONFLICT (external_id) DO NOTHING;
            """;

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(insertSql, customer, cancellationToken: ct));

        return await GetByExternalIdAsync(externalId, ct)
               ?? throw new InvalidOperationException("Customer could not be created or loaded.");
    }
}
