namespace TransactionApi.Application.DTOs;

/// <summary>
/// Represents a transaction returned from query endpoints.
/// </summary>
public sealed class TransactionDto
{
    /// <summary>
    /// External customer identifier that owns the transaction.
    /// </summary>
    public string CustomerId { get; init; } = string.Empty;

    /// <summary>
    /// External transaction identifier used for deduplication.
    /// </summary>
    public string TransactionId { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when the transaction occurred.
    /// </summary>
    public DateTimeOffset TransactionDate { get; init; }

    /// <summary>
    /// Transaction amount.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// ISO 4217 three-letter currency code.
    /// </summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>
    /// Source channel that submitted the transaction.
    /// </summary>
    public string SourceChannel { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when the transaction was stored.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}
