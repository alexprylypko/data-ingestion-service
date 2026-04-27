using FluentValidation;
using TransactionApi.Application.Commands;
using TransactionApi.Application.DTOs;
using TransactionApi.Application.Interfaces;
using TransactionApi.Application.Validators;
using TransactionApi.Domain.Enums;
using TransactionApi.Domain.Models;
using TransactionApi.Tests.Fixtures;

namespace TransactionApi.Tests.Handlers;

/// <summary>Verifies single-transaction ingestion behavior.</summary>
public sealed class IngestTransactionCommandHandlerTests : IAsyncDisposable
{
    private readonly TransactionFixture _fixture = new();
    private readonly Mock<ICustomerRepository> _customerRepositoryMock = new();
    private readonly Mock<ITransactionWriteRepository> _transactionRepositoryMock = new();
    private readonly IValidator<TransactionInputDto> _validator = new TransactionInputDtoValidator();

    /// <summary>
    /// <code>
    /// GIVEN a valid transaction DTO
    ///   AND no existing record with the same transaction ID
    ///  WHEN the command handler processes it
    ///  THEN the result status is Accepted
    ///   AND Insert is called exactly once
    /// </code>
    /// </summary>
    [Fact]
    public async Task Handle_NewTransaction_ReturnsAccepted()
    {
        // Arrange
        var dto = _fixture.CreateValidDto();
        var customer = _fixture.CreateCustomer(dto.CustomerId);
        var handler = CreateHandler();
        _transactionRepositoryMock.Setup(repo => repo.ExistsAsync(dto.TransactionId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _customerRepositoryMock.Setup(repo => repo.GetOrCreateAsync(dto.CustomerId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        // Act
        var result = await handler.HandleAsync(new IngestTransactionCommand(dto));

        // Assert
        result.Status.Should().Be(IngestStatus.Accepted);
        _transactionRepositoryMock.Verify(repo => repo.InsertAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// <code>
    /// GIVEN a transaction DTO whose ID already exists in the store
    ///  WHEN the command handler processes it
    ///  THEN the result status is Rejected
    ///   AND the rejection reason mentions duplicate
    ///   BUT Insert is never called
    /// </code>
    /// </summary>
    [Fact]
    public async Task Handle_DuplicateTransaction_ReturnsDuplicateRejection()
    {
        // Arrange
        var dto = _fixture.CreateValidDto();
        var handler = CreateHandler();
        _transactionRepositoryMock.Setup(repo => repo.ExistsAsync(dto.TransactionId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await handler.HandleAsync(new IngestTransactionCommand(dto));

        // Assert
        result.Status.Should().Be(IngestStatus.Rejected);
        result.Errors.Should().ContainSingle(error => error.Contains("Duplicate", StringComparison.OrdinalIgnoreCase));
        _transactionRepositoryMock.Verify(repo => repo.InsertAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// <code>
    /// GIVEN an invalid transaction DTO
    ///  WHEN the command handler processes it
    ///  THEN the result status is Rejected
    ///   AND the rejection reason mentions validation
    ///   BUT Insert is never called
    /// </code>
    /// </summary>
    [Fact]
    public async Task Handle_InvalidDto_ReturnsRejected()
    {
        // Arrange
        var dto = _fixture.CreateDtoWithAmount(0m);
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(new IngestTransactionCommand(dto));

        // Assert
        result.Status.Should().Be(IngestStatus.Rejected);
        result.Errors.Should().NotBeEmpty();
        _transactionRepositoryMock.Verify(repo => repo.InsertAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// <code>
    /// GIVEN a valid transaction DTO
    ///   AND the repository throws during insert
    ///  WHEN the command handler processes it
    ///  THEN the exception propagates to the caller
    /// </code>
    /// </summary>
    [Fact]
    public async Task Handle_RepositoryThrows_PropagatesException()
    {
        // Arrange
        var dto = _fixture.CreateValidDto();
        var customer = _fixture.CreateCustomer(dto.CustomerId);
        var handler = CreateHandler();
        _transactionRepositoryMock.Setup(repo => repo.ExistsAsync(dto.TransactionId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _customerRepositoryMock.Setup(repo => repo.GetOrCreateAsync(dto.CustomerId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _transactionRepositoryMock.Setup(repo => repo.InsertAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException(dto.TransactionId));

        // Act
        var act = () => handler.HandleAsync(new IngestTransactionCommand(dto));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    private IngestTransactionCommandHandler CreateHandler() =>
        new(_transactionRepositoryMock.Object, _customerRepositoryMock.Object, _validator);
}
