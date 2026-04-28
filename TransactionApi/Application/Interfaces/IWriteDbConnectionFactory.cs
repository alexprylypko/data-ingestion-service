using System.Data;

namespace TransactionApi.Application.Interfaces;

/// <summary>
/// Creates database connections for write (command) operations.
/// Implementations target the primary writable endpoint.
/// </summary>
public interface IWriteDbConnectionFactory
{
    /// <summary>
    /// Opens and returns a new write-side <see cref="IDbConnection"/>.</summary>
    IDbConnection CreateConnection();
}
