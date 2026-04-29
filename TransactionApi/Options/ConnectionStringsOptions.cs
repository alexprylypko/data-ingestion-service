namespace TransactionApi.Options;

/// <summary>
/// Represents the database connection strings required by the application.
/// </summary>
public sealed class ConnectionStringsOptions
{
    /// <summary>
    /// Gets or sets the primary writable database connection string.
    /// </summary>
    public string WriteConnection { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the read-only database connection string.
    /// </summary>
    public string ReadConnection { get; set; } = string.Empty;
}
