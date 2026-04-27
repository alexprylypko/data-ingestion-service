namespace TransactionApi.Application.DTOs;

/// <summary>Represents the aggregate outcome of a streamed batch ingestion request.</summary>
public sealed class BatchIngestResult
{
    /// <summary>Total number of rows processed from the batch file.</summary>
    public int TotalRows { get; set; }

    /// <summary>Total number of accepted rows.</summary>
    public int AcceptedCount { get; set; }

    /// <summary>Total number of rejected rows.</summary>
    public int RejectedCount { get; set; }

    /// <summary>Details describing every rejected row.</summary>
    public List<RejectedRowResult> RejectedRows { get; } = [];
}

/// <summary>Describes a rejected row in a batch ingestion operation.</summary>
public sealed class RejectedRowResult
{
    /// <summary>One-based row number from the CSV file.</summary>
    public int RowNumber { get; init; }

    /// <summary>Transaction identifier associated with the rejected row.</summary>
    public string TransactionId { get; init; } = string.Empty;

    /// <summary>Validation or business-rule errors associated with the rejected row.</summary>
    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();
}
