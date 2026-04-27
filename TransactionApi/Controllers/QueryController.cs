using Microsoft.AspNetCore.Mvc;
using TransactionApi.Application.Queries;

namespace TransactionApi.Controllers;

/// <summary>Exposes read-only query endpoints for customers and aggregate transaction statistics.</summary>
[ApiController]
public sealed class QueryController : ControllerBase
{
    private readonly GetCustomerTransactionsQueryHandler _customerTransactionsHandler;
    private readonly GetSummaryStatsQueryHandler _summaryStatsHandler;

    /// <summary>Initialises the controller with the query handlers it coordinates.</summary>
    public QueryController(
        GetCustomerTransactionsQueryHandler customerTransactionsHandler,
        GetSummaryStatsQueryHandler summaryStatsHandler)
    {
        _customerTransactionsHandler = customerTransactionsHandler;
        _summaryStatsHandler = summaryStatsHandler;
    }

    /// <summary>Returns a paginated list of transactions for the supplied external customer identifier.</summary>
    [HttpGet("customers/{id}/transactions")]
    public async Task<IActionResult> GetCustomerTransactions(
        string id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        [FromQuery] string? currency = null,
        [FromQuery] string? sourceChannel = null,
        CancellationToken ct = default)
    {
        if (fromDate > toDate)
        {
            return BadRequest("fromDate cannot be greater than toDate.");
        }

        var query = new GetCustomerTransactionsQuery
        {
            CustomerId = id,
            Page = Math.Max(page, 1),
            PageSize = Math.Clamp(pageSize, 1, 100),
            FromDate = fromDate,
            ToDate = toDate,
            Currency = currency,
            SourceChannel = sourceChannel
        };

        var result = await _customerTransactionsHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>Returns aggregate summary statistics across all ingested transactions.</summary>
    [HttpGet("stats/summary")]
    public async Task<IActionResult> GetSummaryStats(CancellationToken ct)
    {
        var result = await _summaryStatsHandler.HandleAsync(new GetSummaryStatsQuery(), ct);
        return Ok(result);
    }
}
