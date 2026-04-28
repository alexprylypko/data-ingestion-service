using System.Text;
using Dapper;

namespace TransactionApi.Infrastructure.Data;

/// <summary>
/// Builds SQL clauses and parameters for <see cref="TransactionReadRepository"/>.
/// </summary>
internal static class TransactionReadRepositorySqlBuilder
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int DefaultLookbackMonths = 3;
    private const string IdColumn = "id";
    private const string CustomerIdColumn = "customer_id";
    private const string ExternalTransactionIdColumn = "external_transaction_id";
    private const string TransactionDateColumn = "transaction_date";
    private const string AmountColumn = "amount";
    private const string CurrencyColumn = "currency";
    private const string SourceChannelColumn = "source_channel";
    private const string CreatedAtColumn = "created_at";

    /// <summary>
    /// Builds the paginated customer-transactions query.
    /// </summary>
    /// <param name="customerId">Internal customer identifier.</param>
    /// <param name="page">Requested one-based page number.</param>
    /// <param name="pageSize">Requested page size.</param>
    /// <param name="fromDate">Optional inclusive start date.</param>
    /// <param name="toDate">Optional inclusive end date.</param>
    /// <param name="currency">Optional currency filter.</param>
    /// <param name="sourceChannel">Optional source channel filter.</param>
    /// <returns>Tuple containing query text and Dapper parameters.</returns>
    internal static (string Sql, DynamicParameters Parameters) BuildCustomerTransactionsQuery(
        Guid customerId,
        int page,
        int pageSize,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? currency,
        string? sourceChannel)
    {
        var normalizedFilters = NormalizeFilters(page, pageSize, fromDate, toDate, currency, sourceChannel);
        var sql = new StringBuilder(
            $"""
            SELECT
                {IdColumn} AS Id,
                {CustomerIdColumn} AS CustomerId,
                {ExternalTransactionIdColumn} AS ExternalTransactionId,
                {TransactionDateColumn} AS TransactionDate,
                {AmountColumn} AS Amount,
                {CurrencyColumn} AS Currency,
                {SourceChannelColumn} AS SourceChannel,
                {CreatedAtColumn} AS CreatedAt,
                COUNT(*) OVER() AS TotalCount
            FROM transactions
            WHERE {CustomerIdColumn} = @CustomerId
            """);

        var parameters = CreateBaseCustomerTransactionsParameters(
            customerId,
            normalizedFilters.Page,
            normalizedFilters.PageSize);

        AppendFromDateFilter(sql, parameters, normalizedFilters.FromDate);
        AppendToDateFilter(sql, parameters, normalizedFilters.ToDate);
        AppendCurrencyFilter(sql, parameters, normalizedFilters.Currency);
        AppendSourceChannelFilter(sql, parameters, normalizedFilters.SourceChannel);
        AppendOrderByClause(sql);
        AppendPaginationClause(sql);

        return (sql.ToString(), parameters);
    }

    private static CustomerTransactionsFilters NormalizeFilters(
        int page,
        int pageSize,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? currency,
        string? sourceChannel)
    {
        var effectiveToDate = toDate ?? DateTimeOffset.UtcNow;
        var effectiveFromDate = fromDate ?? effectiveToDate.AddMonths(-DefaultLookbackMonths);

        return new CustomerTransactionsFilters(
            page > 0 ? page : DefaultPage,
            pageSize > 0 ? pageSize : DefaultPageSize,
            effectiveFromDate,
            effectiveToDate,
            NormalizeOptionalFilter(currency),
            NormalizeOptionalFilter(sourceChannel));
    }

    private static string? NormalizeOptionalFilter(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static DynamicParameters CreateBaseCustomerTransactionsParameters(Guid customerId, int page, int pageSize)
    {
        var parameters = new DynamicParameters();
        parameters.Add("CustomerId", customerId);
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);
        return parameters;
    }

    private static void AppendFromDateFilter(StringBuilder sql, DynamicParameters parameters, DateTimeOffset? fromDate)
    {
        if (!fromDate.HasValue)
        {
            return;
        }

        sql.AppendLine($" AND {TransactionDateColumn} >= @FromDate");
        parameters.Add("FromDate", fromDate.Value);
    }

    private static void AppendToDateFilter(StringBuilder sql, DynamicParameters parameters, DateTimeOffset? toDate)
    {
        if (!toDate.HasValue)
        {
            return;
        }

        sql.AppendLine($" AND {TransactionDateColumn} <= @ToDate");
        parameters.Add("ToDate", toDate.Value);
    }

    private static void AppendCurrencyFilter(StringBuilder sql, DynamicParameters parameters, string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return;
        }

        sql.AppendLine($" AND {CurrencyColumn} = @Currency");
        parameters.Add("Currency", currency);
    }

    private static void AppendSourceChannelFilter(StringBuilder sql, DynamicParameters parameters, string? sourceChannel)
    {
        if (string.IsNullOrWhiteSpace(sourceChannel))
        {
            return;
        }

        sql.AppendLine($" AND {SourceChannelColumn} = @SourceChannel");
        parameters.Add("SourceChannel", sourceChannel);
    }

    private static void AppendOrderByClause(StringBuilder sql)
        => sql.AppendLine($" ORDER BY {TransactionDateColumn} DESC, {IdColumn} DESC");

    private static void AppendPaginationClause(StringBuilder sql)
        => sql.AppendLine(" LIMIT @PageSize OFFSET @Offset;");

    private sealed record CustomerTransactionsFilters(
        int Page,
        int PageSize,
        DateTimeOffset FromDate,
        DateTimeOffset ToDate,
        string? Currency,
        string? SourceChannel);
}
