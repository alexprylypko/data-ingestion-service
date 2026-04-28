namespace TransactionApi.Domain.Models;

/// <summary>
/// Represents a financial transaction stored in the system.
/// </summary>
public sealed class Transaction
{
    /// <summary>
    /// Internal surrogate primary key.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key referencing the owning <see cref="Customer"/>.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Caller-supplied identifier used for deduplication.
    /// </summary>
    public string ExternalTransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Point in time the transaction occurred.
    /// </summary>
    public DateTimeOffset TransactionDate { get; set; }

    /// <summary>
    /// Monetary value of the transaction.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// ISO 4217 three-letter currency code (e.g. "USD").
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Channel through which the transaction was submitted.
    /// </summary>
    public string SourceChannel { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the record was persisted.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
