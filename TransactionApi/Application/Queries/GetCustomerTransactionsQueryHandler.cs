using TransactionApi.Application.DTOs;
using TransactionApi.Application.Interfaces;
using TransactionApi.Domain.Exceptions;

namespace TransactionApi.Application.Queries;

/// <summary>Handles paginated transaction queries for a specific customer.</summary>
public sealed class GetCustomerTransactionsQueryHandler
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ITransactionReadRepository _transactionRepository;

    /// <summary>Initialises the handler with the repositories required to resolve and query customer transactions.</summary>
    public GetCustomerTransactionsQueryHandler(
        ITransactionReadRepository transactionRepository,
        ICustomerRepository customerRepository)
    {
        _transactionRepository = transactionRepository;
        _customerRepository = customerRepository;
    }

    /// <summary>Returns a paginated transaction list for the requested external customer identifier.</summary>
    public async Task<PagedResult<TransactionDto>> HandleAsync(GetCustomerTransactionsQuery query, CancellationToken ct = default)
    {
        var customer = await _customerRepository.GetByExternalIdAsync(query.CustomerId, ct);
        if (customer is null)
        {
            throw new NotFoundException($"Customer '{query.CustomerId}' was not found.");
        }

        var (items, totalCount) = await _transactionRepository.GetByCustomerIdAsync(
            customer.Id,
            query.Page,
            query.PageSize,
            query.FromDate,
            query.ToDate,
            query.Currency,
            query.SourceChannel,
            ct);

        var pageItems = items
            .Select(transaction => new TransactionDto
            {
                CustomerId = customer.ExternalId,
                TransactionId = transaction.ExternalTransactionId,
                TransactionDate = transaction.TransactionDate,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                SourceChannel = transaction.SourceChannel,
                CreatedAt = transaction.CreatedAt
            })
            .ToArray();

        return new PagedResult<TransactionDto>
        {
            Items = pageItems,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = query.PageSize == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }
}
