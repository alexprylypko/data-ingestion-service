using TransactionApi.Application.DTOs;

namespace TransactionApi.Application.Commands;

/// <summary>Represents a request to ingest a single real-time transaction.</summary>
public sealed class IngestTransactionCommand
{
    /// <summary>Initialises the command with the submitted transaction payload.</summary>
    public IngestTransactionCommand(TransactionInputDto transaction)
        => Transaction = transaction;

    /// <summary>The submitted transaction payload.</summary>
    public TransactionInputDto Transaction { get; }
}
