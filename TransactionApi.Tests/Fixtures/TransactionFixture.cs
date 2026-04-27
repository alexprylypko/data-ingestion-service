using TransactionApi.Application.DTOs;
using TransactionApi.Application.Queries;
using TransactionApi.Domain.Models;

namespace TransactionApi.Tests.Fixtures;

/// <summary>
/// Provides generated test data for transaction-related tests and
/// cleans up all tracked records on disposal.
/// </summary>
public sealed class TransactionFixture : IAsyncDisposable
{
    private readonly List<string> _trackedTransactionIds = [];

    /// <summary>Creates a <see cref="TransactionInputDto"/> with valid, generated field values.</summary>
    public TransactionInputDto CreateValidDto() =>
        new(
            CreateCustomerExternalId(),
            CreateTransactionExternalId(),
            DateTimeOffset.UtcNow.AddMinutes(-5),
            42.75m,
            "USD",
            "web");

    /// <summary>Creates a valid <see cref="CsvTransactionRow"/> with generated field values.</summary>
    public CsvTransactionRow CreateValidCsvRow()
    {
        var dto = CreateValidDto();
        return new CsvTransactionRow
        {
            CustomerId = dto.CustomerId,
            TransactionId = dto.TransactionId,
            TransactionDate = dto.TransactionDate.ToString("O"),
            Amount = dto.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Currency = dto.Currency,
            SourceChannel = dto.SourceChannel
        };
    }

    /// <summary>Creates a customer instance with generated values.</summary>
    public Customer CreateCustomer(string? externalId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId ?? CreateCustomerExternalId(),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
        };

    /// <summary>Creates a stored transaction model with generated values.</summary>
    public Transaction CreateTransaction(Guid customerId, string? customerExternalId = null)
    {
        var dto = CreateValidDto();
        return new Transaction
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            ExternalTransactionId = dto.TransactionId,
            TransactionDate = dto.TransactionDate,
            Amount = dto.Amount,
            Currency = dto.Currency,
            SourceChannel = dto.SourceChannel,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
    }

    /// <summary>Creates a summary stats DTO with generated aggregate values.</summary>
    public TransactionSummaryStats CreateSummaryStats() =>
        new()
        {
            TotalTransactions = 12,
            TotalAmountUsd = 998.12m,
            UniqueCustomers = 3,
            OldestTransaction = DateTimeOffset.UtcNow.AddDays(-20),
            NewestTransaction = DateTimeOffset.UtcNow.AddDays(-1),
            ByCurrency =
            [
                new CurrencyBreakdown
                {
                    Currency = "USD",
                    Count = 12,
                    TotalAmount = 998.12m
                }
            ],
            ByChannel =
            [
                new ChannelBreakdown
                {
                    Channel = "web",
                    Count = 7
                }
            ]
        };

    /// <summary>Creates a customer-transactions query with generated defaults.</summary>
    public GetCustomerTransactionsQuery CreateCustomerTransactionsQuery(string customerId) =>
        new()
        {
            CustomerId = customerId,
            Page = 1,
            PageSize = 20
        };

    /// <summary>Creates an async sequence from the supplied CSV rows.</summary>
    public IAsyncEnumerable<CsvTransactionRow> CreateAsyncRows(params CsvTransactionRow[] rows) =>
        CreateAsyncRowsCore(rows);

    /// <summary>Creates a large valid CSV row batch for streaming tests.</summary>
    public IAsyncEnumerable<CsvTransactionRow> CreateLargeValidBatch(int count) =>
        CreateAsyncRowsCore(Enumerable.Range(0, count).Select(_ => CreateValidCsvRow()).ToArray());

    /// <summary>Creates a DTO variant with an empty customer identifier.</summary>
    public TransactionInputDto CreateDtoWithEmptyCustomerId() => CreateValidDto() with { CustomerId = string.Empty };

    /// <summary>Creates a DTO variant with the specified amount.</summary>
    public TransactionInputDto CreateDtoWithAmount(decimal amount) => CreateValidDto() with { Amount = amount };

    /// <summary>Creates a DTO variant with the specified currency.</summary>
    public TransactionInputDto CreateDtoWithCurrency(string currency) => CreateValidDto() with { Currency = currency };

    /// <summary>Creates a DTO variant with the specified transaction date.</summary>
    public TransactionInputDto CreateDtoWithTransactionDate(DateTimeOffset transactionDate) =>
        CreateValidDto() with { TransactionDate = transactionDate };

    /// <summary>Creates a DTO variant with the specified source channel.</summary>
    public TransactionInputDto CreateDtoWithSourceChannel(string sourceChannel) =>
        CreateValidDto() with { SourceChannel = sourceChannel };

    /// <summary>Creates a CSV row variant with the specified amount.</summary>
    public CsvTransactionRow CreateCsvRowWithAmount(string amount)
    {
        var row = CreateValidCsvRow();
        row.Amount = amount;
        return row;
    }

    /// <summary>Creates a CSV row variant with the specified currency.</summary>
    public CsvTransactionRow CreateCsvRowWithCurrency(string currency)
    {
        var row = CreateValidCsvRow();
        row.Currency = currency;
        return row;
    }

    /// <summary>Creates a CSV row variant with the specified transaction date.</summary>
    public CsvTransactionRow CreateCsvRowWithTransactionDate(string transactionDate)
    {
        var row = CreateValidCsvRow();
        row.TransactionDate = transactionDate;
        return row;
    }

    /// <summary>Creates a CSV row variant with the specified source channel.</summary>
    public CsvTransactionRow CreateCsvRowWithSourceChannel(string sourceChannel)
    {
        var row = CreateValidCsvRow();
        row.SourceChannel = sourceChannel;
        return row;
    }

    /// <summary>Creates a CSV row variant with the specified transaction identifier.</summary>
    public CsvTransactionRow CreateCsvRowWithTransactionId(string transactionId)
    {
        var row = CreateValidCsvRow();
        row.TransactionId = transactionId;
        return row;
    }

    /// <summary>Tracks a created external transaction ID so it is deleted on disposal.</summary>
    public void Track(string externalTransactionId) => _trackedTransactionIds.Add(externalTransactionId);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _trackedTransactionIds.Clear();
        return ValueTask.CompletedTask;
    }

    private static async IAsyncEnumerable<CsvTransactionRow> CreateAsyncRowsCore(IEnumerable<CsvTransactionRow> rows)
    {
        foreach (var row in rows)
        {
            yield return row;
            await Task.Yield();
        }
    }

    private static string CreateCustomerExternalId() => $"CUST-{Guid.NewGuid():N}"[..18];

    private static string CreateTransactionExternalId() => $"TXN-{Guid.NewGuid():N}";
}
