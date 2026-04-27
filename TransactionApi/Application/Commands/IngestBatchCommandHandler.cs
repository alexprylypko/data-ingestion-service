using System.Globalization;
using FluentValidation;
using TransactionApi.Application.DTOs;
using TransactionApi.Application.Interfaces;
using TransactionApi.Domain.Enums;
using TransactionApi.Domain.Models;

namespace TransactionApi.Application.Commands;

/// <summary>Handles streamed batch ingestion of CSV transaction rows.</summary>
public sealed class IngestBatchCommandHandler
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ITransactionWriteRepository _transactionRepository;
    private readonly IValidator<CsvTransactionRow> _validator;

    /// <summary>Initialises the handler with its required repositories and validator.</summary>
    public IngestBatchCommandHandler(
        ITransactionWriteRepository transactionRepository,
        ICustomerRepository customerRepository,
        IValidator<CsvTransactionRow> validator)
    {
        _transactionRepository = transactionRepository;
        _customerRepository = customerRepository;
        _validator = validator;
    }

    /// <summary>Streams, validates, deduplicates, and persists batch transaction rows.</summary>
    public async Task<BatchIngestResult> HandleAsync(IngestBatchCommand command, CancellationToken ct = default)
    {
        var result = new BatchIngestResult();
        var rowNumber = 0;

        await foreach (var row in command.Rows.WithCancellation(ct))
        {
            rowNumber++;
            result.TotalRows++;

            var validation = await _validator.ValidateAsync(row, ct);
            if (!validation.IsValid)
            {
                RejectRow(result, rowNumber, row.TransactionId, validation.Errors.Select(static error => error.ErrorMessage));
                continue;
            }

            if (await _transactionRepository.ExistsAsync(row.TransactionId, ct))
            {
                RejectRow(result, rowNumber, row.TransactionId, ["Duplicate transaction identifier."]);
                continue;
            }

            var customer = await _customerRepository.GetOrCreateAsync(row.CustomerId, ct);
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                ExternalTransactionId = row.TransactionId,
                TransactionDate = DateTimeOffset.Parse(row.TransactionDate, CultureInfo.InvariantCulture),
                Amount = decimal.Parse(row.Amount, CultureInfo.InvariantCulture),
                Currency = row.Currency,
                SourceChannel = row.SourceChannel,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _transactionRepository.InsertAsync(transaction, ct);
            result.AcceptedCount++;
        }

        return result;
    }

    private static void RejectRow(BatchIngestResult result, int rowNumber, string transactionId, IEnumerable<string> errors)
    {
        result.RejectedCount++;
        result.RejectedRows.Add(new RejectedRowResult
        {
            RowNumber = rowNumber,
            TransactionId = transactionId,
            Errors = errors.ToArray()
        });
    }
}
