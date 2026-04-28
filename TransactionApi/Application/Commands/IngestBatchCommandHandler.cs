using System.Globalization;
using FluentValidation;
using TransactionApi.Application.DTOs;
using TransactionApi.Application.Interfaces;
using TransactionApi.Domain.Enums;
using TransactionApi.Domain.Models;

namespace TransactionApi.Application.Commands;

/// <summary>
/// Handles streamed batch ingestion of CSV transaction rows.
/// </summary>
/// <remarks>
/// <para><strong>Performance model</strong></para>
/// <para>
/// The original handler issued up to 3 database round-trips <em>per row</em>
/// (ExistsAsync + GetOrCreateAsync + InsertAsync), which produces N×3 queries
/// for an N-row CSV.  This refactored version works in chunks:
/// </para>
/// <list type="number">
///   <item>Buffer up to <see cref="ChunkSize"/> rows from the stream.</item>
///   <item>Validate all rows in the chunk concurrently (CPU-bound, semaphore-controlled).</item>
///   <item>Bulk-check duplicates — one query for the whole chunk.</item>
///   <item>Bulk-upsert customers — one query for all distinct customer IDs in the chunk.</item>
///   <item>Bulk-insert transactions — one query for all accepted rows in the chunk.</item>
/// </list>
/// <para>
/// A 10,000-row CSV that previously issued up to 30,000 queries now issues roughly
/// <c>ceil(10_000 / ChunkSize) × 3</c> queries — about 30 for the default chunk size.
/// </para>
/// <para><strong>Chunk size trade-off</strong></para>
/// <para>
/// Smaller chunks reduce peak memory and give earlier partial feedback.
/// Larger chunks reduce DB round-trips further but hold more rows in memory.
/// 500 is a sensible default; override via the constructor for tuning or tests.
/// </para>
/// </remarks>
public sealed class IngestBatchCommandHandler
{
    /// <summary>Default number of rows processed per database round-trip cycle.</summary>
    public const int ChunkSize = 500;

    /// <summary>
    /// Maximum degree of parallelism used when validating rows within a chunk.
    /// Keeps CPU load predictable under concurrent API requests.
    /// </summary>
    private const int MaxValidationConcurrency = 8;

    private readonly ICustomerRepository _customerRepository;
    private readonly ITransactionWriteRepository _transactionRepository;
    private readonly IValidator<CsvTransactionRow> _validator;
    private readonly int _chunkSize;

    /// <summary>
    /// Initializes the handler with its required repositories and validator.
    /// </summary>
    /// <param name="transactionRepository">Write-side transaction repository.</param>
    /// <param name="customerRepository">Customer read/write repository.</param>
    /// <param name="validator">Row-level CSV validator.</param>
    /// <param name="chunkSize">
    /// Optional chunk size override.  Defaults to <see cref="ChunkSize"/>.
    /// Inject a smaller value in tests to exercise chunk-boundary logic.
    /// </param>
    public IngestBatchCommandHandler(
        ITransactionWriteRepository transactionRepository,
        ICustomerRepository customerRepository,
        IValidator<CsvTransactionRow> validator,
        int chunkSize = ChunkSize)
    {
        _transactionRepository = transactionRepository;
        _customerRepository = customerRepository;
        _validator = validator;
        _chunkSize = chunkSize > 0
            ? chunkSize
            : throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be greater than zero.");
    }

    /// <summary>
    /// Streams, validates, deduplicates, and persists batch transaction rows
    /// using a chunked pipeline to minimise database round-trips.
    /// </summary>
    public async Task<BatchIngestResult> HandleAsync(
        IngestBatchCommand command,
        CancellationToken ct = default)
    {
        var result = new BatchIngestResult();
        var rowNumber = 0;

        await foreach (var chunk in ReadChunksAsync(command.Rows, ct))
        {
            await ProcessChunkAsync(chunk, result, rowNumber, ct);
        }

        return result;
    }

    // -------------------------------------------------------------------------
    // Chunked streaming
    // -------------------------------------------------------------------------

    /// <summary>
    /// Buffers the async stream into fixed-size chunks without materialising
    /// the entire sequence in memory at once.
    /// </summary>
    private async IAsyncEnumerable<List<CsvTransactionRow>> ReadChunksAsync(
        IAsyncEnumerable<CsvTransactionRow> rows,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var chunk = new List<CsvTransactionRow>(_chunkSize);

        await foreach (var row in rows.WithCancellation(ct))
        {
            chunk.Add(row);

            if (chunk.Count < _chunkSize)
            {
                continue;
            }

            yield return chunk;
            chunk = new List<CsvTransactionRow>(_chunkSize);
        }

        if (chunk.Count > 0)
        {
            yield return chunk;
        }
    }

    // -------------------------------------------------------------------------
    // Chunk processing — 3 DB round-trips total per chunk
    // -------------------------------------------------------------------------

    private async Task ProcessChunkAsync(
        List<CsvTransactionRow> chunk,
        BatchIngestResult result,
        int rowNumber,
        CancellationToken ct)
    {
        result.TotalRows += chunk.Count;

        // Step 1 — validate all rows in parallel (CPU-bound, bounded concurrency)
        var validatedRows = await ValidateChunkAsync(chunk, rowNumber, ct);
        rowNumber += chunk.Count;

        // Separate valid from already-rejected rows
        var validRows = validatedRows
            .Where(static r => r.IsValid)
            .ToList();

        foreach (var rejected in validatedRows.Where(static r => !r.IsValid))
        {
            AddRejected(result, rejected.RowNumber, rejected.Row.TransactionId, rejected.Errors!);
        }

        if (validRows.Count == 0)
        {
            return;
        }

        // Step 2 — bulk-check duplicates: 1 query for the whole chunk
        var transactionIds = validRows.Select(static r => r.Row.TransactionId).ToList();
        var existingIds = await _transactionRepository.GetExistingIdsAsync(transactionIds, ct);

        var deduplicatedRows = new List<ValidatedRow>(validRows.Count);
        foreach (var row in validRows)
        {
            if (existingIds.Contains(row.Row.TransactionId))
            {
                AddRejected(result, row.RowNumber, row.Row.TransactionId,
                    ["Duplicate transaction identifier."]);
            }
            else
            {
                deduplicatedRows.Add(row);
            }
        }

        if (deduplicatedRows.Count == 0)
        {
            return;
        }

        // Step 3 — parse amounts/dates (safe — validation already confirmed the formats)
        var parsedRows = new List<(ValidatedRow Meta, DateTimeOffset Date, decimal Amount)>(deduplicatedRows.Count);
        foreach (var row in deduplicatedRows)
        {
            if (!TryParseRow(row.Row, out var date, out var amount, out var parseError))
            {
                AddRejected(result, row.RowNumber, row.Row.TransactionId, [parseError]);
                continue;
            }

            parsedRows.Add((row, date, amount));
        }

        if (parsedRows.Count == 0)
        {
            return;
        }

        // Step 4 — bulk-upsert customers: 1 query for all distinct customer IDs
        var distinctCustomerIds = parsedRows
            .Select(static r => r.Meta.Row.CustomerId)
            .Distinct()
            .ToList();
        var customerMap = await _customerRepository.BulkGetOrCreateAsync(distinctCustomerIds, ct);

        // Step 5 — build transaction entities
        var transactions = new List<Transaction>(parsedRows.Count);
        foreach (var (meta, date, amount) in parsedRows)
        {
            if (!customerMap.TryGetValue(meta.Row.CustomerId, out var customer))
            {
                // Defensive — BulkGetOrCreateAsync guarantees all requested IDs are present
                AddRejected(result, meta.RowNumber, meta.Row.TransactionId,
                    [$"Customer '{meta.Row.CustomerId}' could not be resolved."]);
                continue;
            }

            transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                ExternalTransactionId = meta.Row.TransactionId,
                TransactionDate = date,
                Amount = amount,
                Currency = meta.Row.Currency,
                SourceChannel = meta.Row.SourceChannel,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        // Step 6 — bulk-insert transactions: 1 query for the whole chunk
        await _transactionRepository.BulkInsertAsync(transactions, ct);
        result.AcceptedCount += transactions.Count;
    }

    // -------------------------------------------------------------------------
    // Parallel validation
    // -------------------------------------------------------------------------

    private async Task<List<ValidatedRow>> ValidateChunkAsync(
        List<CsvTransactionRow> chunk,
        int baseRowNumber,
        CancellationToken ct)
    {
        var results = new ValidatedRow[chunk.Count];
        var semaphore = new SemaphoreSlim(MaxValidationConcurrency, MaxValidationConcurrency);

        var tasks = chunk.Select(async (row, index) =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var rowNumber = baseRowNumber + index + 1;
                var validation = await _validator.ValidateAsync(row, ct);

                results[index] = validation.IsValid
                    ? new ValidatedRow(rowNumber, row, IsValid: true, Errors: null)
                    : new ValidatedRow(
                        rowNumber,
                        row,
                        IsValid: false,
                        Errors: validation.Errors
                            .Select(static e => e.ErrorMessage)
                            .ToArray());
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return [.. results];
    }

    // -------------------------------------------------------------------------
    // Safe parsing helpers
    // -------------------------------------------------------------------------

    private static bool TryParseRow(
        CsvTransactionRow row,
        out DateTimeOffset date,
        out decimal amount,
        out string error)
    {
        date = default;
        amount = default;
        error = string.Empty;

        if (!DateTimeOffset.TryParse(
                row.TransactionDate,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out date))
        {
            error = $"'{row.TransactionDate}' is not a valid date/time.";
            return false;
        }

        if (!decimal.TryParse(
                row.Amount,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out amount))
        {
            error = $"'{row.Amount}' is not a valid decimal amount.";
            return false;
        }

        return true;
    }

    // -------------------------------------------------------------------------
    // Result helpers
    // -------------------------------------------------------------------------

    private static void AddRejected(
        BatchIngestResult result,
        int rowNumber,
        string transactionId,
        IReadOnlyCollection<string> errors)
    {
        result.RejectedCount++;
        result.RejectedRows.Add(new RejectedRowResult
        {
            RowNumber = rowNumber,
            TransactionId = transactionId,
            Errors = errors
        });
    }

    // -------------------------------------------------------------------------
    // Private value types
    // -------------------------------------------------------------------------

    private sealed record ValidatedRow(
        int RowNumber,
        CsvTransactionRow Row,
        bool IsValid,
        string[]? Errors);
}
