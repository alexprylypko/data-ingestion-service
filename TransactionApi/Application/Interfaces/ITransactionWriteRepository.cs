using TransactionApi.Domain.Models;

namespace TransactionApi.Application.Interfaces;

/// <summary>Write operations for the <see cref="Transaction"/> aggregate.</summary>
public interface ITransactionWriteRepository
{
    /// <summary>Returns <c>true</c> when a transaction with <paramref name="externalTransactionId"/> already exists.</summary>
    Task<bool> ExistsAsync(string externalTransactionId, CancellationToken ct = default);

    /// <summary>Persists a new <paramref name="transaction"/> record.</summary>
    Task InsertAsync(Transaction transaction, CancellationToken ct = default);
}
