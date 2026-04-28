using TransactionApi.Domain.Models;

namespace TransactionApi.Application.Interfaces;

/// <summary>
/// Read/write access to customer records.
/// </summary>
public interface ICustomerRepository
{
    /// <summary>
    /// Returns the customer whose <c>external_id</c> matches <paramref name="externalId"/>, or <c>null</c>.
    /// </summary>
    Task<Customer?> GetByExternalIdAsync(string externalId, CancellationToken ct = default);

    /// <summary>
    /// Returns the existing customer or creates and returns a new one atomically.
    /// </summary>
    Task<Customer> GetOrCreateAsync(string externalId, CancellationToken ct = default);
}
