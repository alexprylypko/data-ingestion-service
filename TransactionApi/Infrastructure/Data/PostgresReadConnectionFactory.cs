using System.Data;
using Npgsql;
using TransactionApi.Application.Interfaces;

namespace TransactionApi.Infrastructure.Data;

/// <summary>
/// PostgreSQL implementation of <see cref="IReadDbConnectionFactory"/>.
/// This is the only class referencing <c>Npgsql</c> on the read path.
/// Replace with a SQL Server or MySQL equivalent to switch providers.
/// </summary>
public sealed class PostgresReadConnectionFactory : IReadDbConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes the factory with the read-replica connection string.
    /// </summary>
    public PostgresReadConnectionFactory(string connectionString)
        => _connectionString = connectionString;

    /// <inheritdoc />
    public IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);
}
