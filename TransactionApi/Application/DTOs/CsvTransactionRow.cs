using CsvHelper.Configuration;

namespace TransactionApi.Application.DTOs;

/// <summary>Represents a CSV row describing a single transaction ingestion request.</summary>
public sealed class CsvTransactionRow
{
    /// <summary>Caller-supplied customer identifier.</summary>
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>Caller-supplied unique transaction identifier.</summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Transaction date as provided in the CSV source.</summary>
    public string TransactionDate { get; set; } = string.Empty;

    /// <summary>Monetary amount as provided in the CSV source.</summary>
    public string Amount { get; set; } = string.Empty;

    /// <summary>ISO 4217 three-letter currency code.</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>Channel through which the transaction was submitted.</summary>
    public string SourceChannel { get; set; } = string.Empty;
}

/// <summary>Maps CSV column names to <see cref="CsvTransactionRow"/> properties.</summary>
public sealed class CsvTransactionRowMap : ClassMap<CsvTransactionRow>
{
    /// <summary>Initialises the CSV column map.</summary>
    public CsvTransactionRowMap()
    {
        Map(m => m.CustomerId).Name("customerId");
        Map(m => m.TransactionId).Name("transactionId");
        Map(m => m.TransactionDate).Name("transactionDate");
        Map(m => m.Amount).Name("amount");
        Map(m => m.Currency).Name("currency");
        Map(m => m.SourceChannel).Name("sourceChannel");
    }
}
