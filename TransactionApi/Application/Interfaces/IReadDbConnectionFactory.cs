using System.Data;

namespace TransactionApi.Application.Interfaces;

/// <summary>
/// Creates database connections for read (query) operations.
/// Implementations target a read-replica or reporting endpoint.
/// </summary>
public interface IReadDbConnectionFactory
{
    /// <summary>
    /// Opens and returns a new read-side <see cref="IDbConnection"/>.
    /// </summary>
    IDbConnection CreateConnection();
}
