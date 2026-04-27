namespace TransactionApi.Domain.Enums;

/// <summary>Describes the outcome of an ingestion attempt.</summary>
public enum IngestStatus
{
    /// <summary>The transaction was accepted and stored.</summary>
    Accepted = 1,

    /// <summary>The transaction was rejected and not stored.</summary>
    Rejected = 2
}
