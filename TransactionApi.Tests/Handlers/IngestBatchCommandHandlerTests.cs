using FluentValidation;
using TransactionApi.Application.Commands;
using TransactionApi.Application.DTOs;
using TransactionApi.Application.Interfaces;
using TransactionApi.Application.Validators;
using TransactionApi.Domain.Models;
using TransactionApi.Tests.Fixtures;

namespace TransactionApi.Tests.Handlers;

/// <summary>Verifies streamed batch ingestion behavior.</summary>
public sealed class IngestBatchCommandHandlerTests : IAsyncDisposable
{
    private readonly TransactionFixture _fixture = new();
    private readonly Mock<ICustomerRepository> _customerRepositoryMock = new();
    private readonly Mock<ITransactionWriteRepository> _transactionRepositoryMock = new();
    private readonly IValidator<CsvTransactionRow> _validator = new CsvTransactionRowValidator();

    /// <summary>
    /// <code>
    /// GIVEN three valid CSV transaction rows
    ///  WHEN the batch handler processes them
    ///  THEN the accepted count is three
    ///   AND the rejected count is zero
    /// </code>
    /// </summary>
    [Fact]
    public async Task Handle_ThreeValidRows_AcceptsAll()
    {
        // Arrange
        var rowOne = _fixture.CreateValidCsvRow();
        var rowTwo = _fixture.CreateValidCsvRow();
        var rowThree = _fixture.CreateValidCsvRow();
        var customerOne = _fixture.CreateCustomer(rowOne.CustomerId);
        var customerTwo = _fixture.CreateCustomer(rowTwo.CustomerId);
        var customerThree = _fixture.CreateCustomer(rowThree.CustomerId);
        var handler = CreateHandler();
        SetupExistingIds();
        SetupBulkCustomers((customerOne, customerOne.ExternalId), (customerTwo, customerTwo.ExternalId), (customerThree, customerThree.ExternalId));

        // Act
        var result = await handler.HandleAsync(new IngestBatchCommand(_fixture.CreateAsyncRows(rowOne, rowTwo, rowThree)));

        // Assert
        result.AcceptedCount.Should().Be(3);
        result.RejectedCount.Should().Be(0);
    }

    /// <summary>
    /// <code>
    /// GIVEN two valid CSV transaction rows
    ///   AND one duplicate transaction row
    ///  WHEN the batch handler processes them
    ///  THEN the accepted count is two
    ///   AND the rejected count is one
    ///   AND the rejection reason mentions duplicate
    /// </code>
    /// </summary>
    [Fact]
    public async Task Handle_DuplicateRow_RejectsDuplicate()
    {
        // Arrange
        var rowOne = _fixture.CreateValidCsvRow();
        var rowTwo = _fixture.CreateValidCsvRow();
        var rowThree = _fixture.CreateValidCsvRow();
        var customerOne = _fixture.CreateCustomer(rowOne.CustomerId);
        var customerTwo = _fixture.CreateCustomer(rowTwo.CustomerId);
        var handler = CreateHandler();
        SetupExistingIds(rowThree.TransactionId);
        SetupBulkCustomers((customerOne, customerOne.ExternalId), (customerTwo, customerTwo.ExternalId));

        // Act
        var result = await handler.HandleAsync(new IngestBatchCommand(_fixture.CreateAsyncRows(rowOne, rowTwo, rowThree)));

        // Assert
        result.AcceptedCount.Should().Be(2);
        result.RejectedCount.Should().Be(1);
        result.RejectedRows.Single().Errors.Should().ContainSingle(error => error.Contains("Duplicate", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// <code>
    /// GIVEN two valid CSV transaction rows
    ///   AND one row with a zero amount
    ///  WHEN the batch handler processes them
    ///  THEN the accepted count is two
    ///   AND the rejected count is one
    ///   AND the rejection reason mentions amount
    /// </code>
    /// </summary>
    [Fact]
    public async Task Handle_InvalidRow_RejectsValidationFailure()
    {
        // Arrange
        var rowOne = _fixture.CreateValidCsvRow();
        var rowTwo = _fixture.CreateValidCsvRow();
        var rowThree = _fixture.CreateCsvRowWithAmount("0");
        var customerOne = _fixture.CreateCustomer(rowOne.CustomerId);
        var customerTwo = _fixture.CreateCustomer(rowTwo.CustomerId);
        var handler = CreateHandler();
        SetupExistingIds();
        SetupBulkCustomers((customerOne, customerOne.ExternalId), (customerTwo, customerTwo.ExternalId));

        // Act
        var result = await handler.HandleAsync(new IngestBatchCommand(_fixture.CreateAsyncRows(rowOne, rowTwo, rowThree)));

        // Assert
        result.AcceptedCount.Should().Be(2);
        result.RejectedCount.Should().Be(1);
        result.RejectedRows.Single().Errors.Should().ContainSingle(error => error.Contains("greater than 0", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// <code>
    /// GIVEN an empty CSV transaction batch
    ///  WHEN the batch handler processes it
    ///  THEN the accepted count is zero
    ///   AND the rejected count is zero
    /// </code>
    /// </summary>
    [Fact]
    public async Task Handle_EmptyBatch_ReturnsZeroCounts()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(new IngestBatchCommand(_fixture.CreateAsyncRows()));

        // Assert
        result.AcceptedCount.Should().Be(0);
        result.RejectedCount.Should().Be(0);
    }

    /// <summary>
    /// <code>
    /// GIVEN one thousand valid CSV transaction rows
    ///  WHEN the batch handler processes them
    ///  THEN Insert is called exactly one thousand times
    /// </code>
    /// </summary>
    [Fact]
    public async Task Handle_ThousandValidRows_InsertsEachRow()
    {
        // Arrange
        var rows = _fixture.CreateLargeValidBatch(1000);
        var handler = CreateHandler();
        SetupExistingIds();
        _customerRepositoryMock
            .Setup(repo => repo.BulkGetOrCreateAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> customerIds, CancellationToken _) =>
                customerIds.Distinct().ToDictionary(customerId => customerId, customerId => _fixture.CreateCustomer(customerId)));

        // Act
        var result = await handler.HandleAsync(new IngestBatchCommand(rows));

        // Assert
        result.AcceptedCount.Should().Be(1000);
        _transactionRepositoryMock.Verify(repo => repo.BulkInsertAsync(It.Is<IReadOnlyCollection<Transaction>>(items => items.Count == 500), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    private IngestBatchCommandHandler CreateHandler() =>
        new(_transactionRepositoryMock.Object, _customerRepositoryMock.Object, _validator);

    private void SetupExistingIds(params string[] existingIds) =>
        _transactionRepositoryMock
            .Setup(repo => repo.GetExistingIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string>(existingIds, StringComparer.Ordinal));

    private void SetupBulkCustomers(params (Customer Customer, string ExternalId)[] customers) =>
        _customerRepositoryMock
            .Setup(repo => repo.BulkGetOrCreateAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers.ToDictionary(item => item.ExternalId, item => item.Customer));
}
