namespace TransactionApi.Application.DTOs;

/// <summary>Represents aggregate reporting data across all stored transactions.</summary>
public sealed class TransactionSummaryStats
{
    /// <summary>Total number of transactions stored in the system.</summary>
    public int TotalTransactions { get; set; }

    /// <summary>Total monetary amount across all transactions.</summary>
    public decimal TotalAmountUsd { get; set; }

    /// <summary>Total number of distinct customers with at least one transaction.</summary>
    public int UniqueCustomers { get; set; }

    /// <summary>Timestamp of the oldest transaction in the data set.</summary>
    public DateTimeOffset? OldestTransaction { get; set; }

    /// <summary>Timestamp of the newest transaction in the data set.</summary>
    public DateTimeOffset? NewestTransaction { get; set; }

    /// <summary>Breakdown of stored transactions by currency.</summary>
    public IReadOnlyCollection<CurrencyBreakdown> ByCurrency { get; set; } = Array.Empty<CurrencyBreakdown>();

    /// <summary>Breakdown of stored transactions by source channel.</summary>
    public IReadOnlyCollection<ChannelBreakdown> ByChannel { get; set; } = Array.Empty<ChannelBreakdown>();
}

/// <summary>Represents a currency-level transaction summary.</summary>
public sealed class CurrencyBreakdown
{
    /// <summary>ISO 4217 three-letter currency code.</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>Total number of transactions in the currency bucket.</summary>
    public int Count { get; set; }

    /// <summary>Total amount represented by the currency bucket.</summary>
    public decimal TotalAmount { get; set; }
}

/// <summary>Represents a source-channel transaction summary.</summary>
public sealed class ChannelBreakdown
{
    /// <summary>Name of the source channel bucket.</summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>Total number of transactions in the channel bucket.</summary>
    public int Count { get; set; }
}
