using System.Data;
using Npgsql;
using TransactionApi.Application.Interfaces;

namespace TransactionApi.Infrastructure.Data;

/// <summary>
/// PostgreSQL implementation of <see cref="IWriteDbConnectionFactory"/>.
/// This is the only class referencing <c>Npgsql</c> on the write path.
/// Replace with a SQL Server or MySQL equivalent to switch providers.
/// </summary>
public sealed class PostgresWriteConnectionFactory : IWriteDbConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>Initialises the factory with the primary write connection string.</summary>
    public PostgresWriteConnectionFactory(string connectionString)
        => _connectionString = connectionString;

    /// <inheritdoc />
    public IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);
}
