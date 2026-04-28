using FluentValidation;
using TransactionApi.Application.DTOs;
using TransactionApi.Application.Interfaces;
using TransactionApi.Domain.Enums;
using TransactionApi.Domain.Models;

namespace TransactionApi.Application.Commands;

/// <summary>
/// Handles ingestion of a single transaction submitted through the real-time endpoint.
/// </summary>
public sealed class IngestTransactionCommandHandler
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ITransactionWriteRepository _transactionRepository;
    private readonly IValidator<TransactionInputDto> _validator;

    /// <summary>
    /// Initializes the handler with its required repositories and validator.
    /// </summary>
    public IngestTransactionCommandHandler(
        ITransactionWriteRepository transactionRepository,
        ICustomerRepository customerRepository,
        IValidator<TransactionInputDto> validator)
    {
        _transactionRepository = transactionRepository;
        _customerRepository = customerRepository;
        _validator = validator;
    }

    /// <summary>
    /// Validates, deduplicates, and persists a single transaction request.
    /// </summary>
    public async Task<RowIngestResult> HandleAsync(IngestTransactionCommand command, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(command.Transaction, ct);
        if (!validation.IsValid)
        {
            return new RowIngestResult
            {
                Status = IngestStatus.Rejected,
                TransactionId = command.Transaction.TransactionId,
                Errors = validation.Errors.Select(static error => error.ErrorMessage).ToArray()
            };
        }

        if (await _transactionRepository.ExistsAsync(command.Transaction.TransactionId, ct))
        {
            return new RowIngestResult
            {
                Status = IngestStatus.Rejected,
                TransactionId = command.Transaction.TransactionId,
                Errors = ["Duplicate transaction identifier."]
            };
        }

        var customer = await _customerRepository.GetOrCreateAsync(command.Transaction.CustomerId, ct);
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            ExternalTransactionId = command.Transaction.TransactionId,
            TransactionDate = command.Transaction.TransactionDate,
            Amount = command.Transaction.Amount,
            Currency = command.Transaction.Currency,
            SourceChannel = command.Transaction.SourceChannel,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _transactionRepository.InsertAsync(transaction, ct);

        return new RowIngestResult
        {
            Status = IngestStatus.Accepted,
            TransactionId = command.Transaction.TransactionId
        };
    }
}
