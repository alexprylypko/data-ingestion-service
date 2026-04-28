namespace TransactionApi.Domain.Models;

/// <summary>
/// Represents a customer that owns one or more ingested transactions.
/// </summary>
public sealed class Customer
{
    /// <summary>
    /// Internal surrogate primary key.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Caller-supplied external identifier for the customer.
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the customer record was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
