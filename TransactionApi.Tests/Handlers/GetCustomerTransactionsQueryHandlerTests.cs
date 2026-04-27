using TransactionApi.Application.Interfaces;
using TransactionApi.Application.Queries;
using TransactionApi.Domain.Exceptions;
using TransactionApi.Domain.Models;
using TransactionApi.Tests.Fixtures;

namespace TransactionApi.Tests.Handlers;

/// <summary>Verifies customer-transaction query behavior.</summary>
public sealed class GetCustomerTransactionsQueryHandlerTests : IAsyncDisposable
{
    private readonly TransactionFixture _fixture = new();
    private readonly Mock<ICustomerRepository> _customerRepositoryMock = new();
    private readonly Mock<ITransactionReadRepository> _transactionRepositoryMock = new();

    /// <summary>
    /// <code>
    /// GIVEN a known customer with stored transactions
    ///  WHEN the query handler processes the request
    ///  THEN the paged result contains the expected transaction
    /// </code>
    /// </summary>
    [Fact]
    public async Task Handle_KnownCustomer_ReturnsPagedResult()
    {
        // Arrange
        var customer = _fixture.CreateCustomer();
        var transaction = _fixture.CreateTransaction(customer.Id, customer.ExternalId);
        var query = _fixture.CreateCustomerTransactionsQuery(customer.ExternalId);
        var handler = new GetCustomerTransactionsQueryHandler(_transactionRepositoryMock.Object, _customerRepositoryMock.Object);
        _customerRepositoryMock.Setup(repo => repo.GetByExternalIdAsync(customer.ExternalId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _transactionRepositoryMock
            .Setup(repo => repo.GetByCustomerIdAsync(customer.Id, query.Page, query.PageSize, query.FromDate, query.ToDate, query.Currency, query.SourceChannel, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { transaction }, 1));

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Items.Should().ContainSingle(item => item.TransactionId == transaction.ExternalTransactionId);
    }

    /// <summary>
    /// <code>
    /// GIVEN an unknown customer external identifier
    ///  WHEN the query handler processes the request
    ///  THEN a not found exception is thrown
    /// </code>
    /// </summary>
    [Fact]
    public async Task Handle_UnknownCustomer_ThrowsNotFoundException()
    {
        // Arrange
        var customer = _fixture.CreateCustomer();
        var query = _fixture.CreateCustomerTransactionsQuery(customer.ExternalId);
        var handler = new GetCustomerTransactionsQueryHandler(_transactionRepositoryMock.Object, _customerRepositoryMock.Object);
        _customerRepositoryMock.Setup(repo => repo.GetByExternalIdAsync(customer.ExternalId, It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        // Act
        var act = () => handler.HandleAsync(query);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _fixture.DisposeAsync();
}
