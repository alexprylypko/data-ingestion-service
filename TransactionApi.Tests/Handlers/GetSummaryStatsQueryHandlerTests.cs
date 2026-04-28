using TransactionApi.Application.Interfaces;
using TransactionApi.Application.Queries;
using TransactionApi.Tests.Fixtures;

namespace TransactionApi.Tests.Handlers;

/// <summary>Verifies summary-stats query behavior.</summary>
public sealed class GetSummaryStatsQueryHandlerTests : IAsyncDisposable
{
    private readonly TransactionFixture _fixture = new();
    private readonly Mock<ITransactionReadRepository> _transactionRepositoryMock = new();

    /// <summary>
    /// <code>
    /// GIVEN stored aggregate transaction statistics
    ///  WHEN the query handler processes the request
    ///  THEN the same summary stats are returned
    /// </code>
    /// </summary>
    [Fact]
    public async Task Handle_ReturnsSummaryStats()
    {
        // Arrange
        var summary = _fixture.CreateSummaryStats();
        var handler = new GetSummaryStatsQueryHandler(_transactionRepositoryMock.Object);
        _transactionRepositoryMock.Setup(repo => repo.GetSummaryStatsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(summary);

        // Act
        var result = await handler.HandleAsync(new GetSummaryStatsQuery());

        // Assert
        result.TotalTransactions.Should().Be(summary.TotalTransactions);
        result.ByChannel.Should().ContainSingle(item => item.TotalAmount == summary.ByChannel.Single().TotalAmount);
        result.ByCustomerCurrency.Should().ContainSingle(item => item.CustomerId == summary.ByCustomerCurrency.Single().CustomerId);
        result.ByCustomerChannel.Should().ContainSingle(item => item.Channel == summary.ByCustomerChannel.Single().Channel);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _fixture.DisposeAsync();
}
