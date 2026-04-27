using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using TransactionApi.Application.Commands;
using TransactionApi.Application.DTOs;
using TransactionApi.Domain.Enums;

namespace TransactionApi.Controllers;

/// <summary>Exposes transaction ingestion endpoints for real-time and batch workflows.</summary>
[ApiController]
[Route("ingest")]
public sealed class IngestController : ControllerBase
{
    private const long MaxFileSizeBytes = 104_857_600;
    private readonly IngestBatchCommandHandler _batchHandler;
    private readonly IngestTransactionCommandHandler _transactionHandler;

    /// <summary>Initialises the controller with the ingestion handlers it coordinates.</summary>
    public IngestController(
        IngestTransactionCommandHandler transactionHandler,
        IngestBatchCommandHandler batchHandler)
    {
        _transactionHandler = transactionHandler;
        _batchHandler = batchHandler;
    }

    /// <summary>Accepts and ingests a single transaction submitted as JSON.</summary>
    [HttpPost("transaction")]
    [ProducesResponseType(typeof(RowIngestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> IngestTransaction([FromBody] TransactionInputDto input, CancellationToken ct)
    {
        var result = await _transactionHandler.HandleAsync(new IngestTransactionCommand(input), ct);
        if (result.Status == IngestStatus.Accepted)
        {
            return Ok(result);
        }

        if (result.Errors.Any(static error => error.Contains("duplicate", StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict(result);
        }

        return UnprocessableEntity(new
        {
            type = "validation_error",
            message = "One or more validation errors occurred.",
            errors = result.Errors
        });
    }

    /// <summary>Accepts and ingests a streamed CSV batch without loading the file into memory.</summary>
    [HttpPost("batch")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(BatchIngestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IngestBatch([FromForm] IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
        {
            return BadRequest("The uploaded file is empty.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return BadRequest("The uploaded file exceeds the 100 MB limit.");
        }

        if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("The uploaded file must have a .csv extension.");
        }

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            MissingFieldFound = null,
            HeaderValidated = null
        });

        csv.Context.RegisterClassMap<CsvTransactionRowMap>();
        var records = csv.GetRecordsAsync<CsvTransactionRow>();
        var result = await _batchHandler.HandleAsync(new IngestBatchCommand(records), ct);
        return Ok(result);
    }
}
