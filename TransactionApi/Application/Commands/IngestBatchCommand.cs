using TransactionApi.Application.DTOs;

namespace TransactionApi.Application.Commands;

/// <summary>Represents a request to ingest a streamed batch of CSV transactions.</summary>
public sealed class IngestBatchCommand
{
    /// <summary>Initialises the command with the streamed CSV rows.</summary>
    public IngestBatchCommand(IAsyncEnumerable<CsvTransactionRow> rows)
        => Rows = rows;

    /// <summary>The streamed CSV rows to ingest without materialising them all in memory.</summary>
    public IAsyncEnumerable<CsvTransactionRow> Rows { get; }
}
