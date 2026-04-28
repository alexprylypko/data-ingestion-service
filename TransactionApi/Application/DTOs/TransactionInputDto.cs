namespace TransactionApi.Application.DTOs;

/// <summary>
/// Represents a single transaction submitted via the real-time API endpoint.
/// </summary>
/// <param name="CustomerId">Caller-supplied customer identifier.</param>
/// <param name="TransactionId">Globally unique transaction identifier used for deduplication.</param>
/// <param name="TransactionDate">Point in time the transaction occurred.</param>
/// <param name="Amount">Monetary value; must be positive.</param>
/// <param name="Currency">ISO 4217 three-letter currency code.</param>
/// <param name="SourceChannel">Channel through which the transaction was submitted.</param>
public record TransactionInputDto(
    string CustomerId,
    string TransactionId,
    DateTimeOffset TransactionDate,
    decimal Amount,
    string Currency,
    string SourceChannel);
