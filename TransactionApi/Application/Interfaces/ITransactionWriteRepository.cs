using TransactionApi.Domain.Models;

namespace TransactionApi.Application.Interfaces;

/// <summary>
/// Write operations for the <see cref="Transaction"/> aggregate.
/// </summary>
public interface ITransactionWriteRepository
{
  /// <summary>
  /// Returns <c>true</c> when a transaction with <paramref name="externalTransactionId"/> already exists.
  /// </summary>
  Task<bool> ExistsAsync(string externalTransactionId, CancellationToken ct = default);

  /// <summary>
  /// Persists a new <paramref name="transaction"/> record.
  /// </summary>
  Task InsertAsync(Transaction transaction, CancellationToken ct = default);

  /// <summary>
  /// Returns the subset of <paramref name="externalIds"/> that already exist in the store.
  /// Used by batch ingestion to eliminate duplicate rows in one round-trip.
  /// </summary>
  Task<IReadOnlySet<string>> GetExistingIdsAsync(
    IEnumerable<string> externalIds,
    CancellationToken ct = default);

  /// <summary>
  /// Inserts all <paramref name="transactions"/> in a single bulk operation.
  /// Rows whose <c>external_transaction_id</c> already exist are silently skipped
  /// (ON CONFLICT DO NOTHING) so callers need not pre-filter them.
  /// </summary>
  Task BulkInsertAsync(IReadOnlyCollection<Transaction> transactions, CancellationToken ct = default);
}