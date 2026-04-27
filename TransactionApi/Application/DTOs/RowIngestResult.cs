using TransactionApi.Domain.Enums;

namespace TransactionApi.Application.DTOs;

/// <summary>Represents the outcome of ingesting a single transaction.</summary>
public sealed class RowIngestResult
{
    /// <summary>The ingestion outcome classification.</summary>
    public IngestStatus Status { get; init; }

    /// <summary>The external transaction identifier tied to the attempt.</summary>
    public string TransactionId { get; init; } = string.Empty;

    /// <summary>Validation or business-rule errors associated with the attempt.</summary>
    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();
}
