using TransactionApi.Application.Validators;
using TransactionApi.Tests.Fixtures;

namespace TransactionApi.Tests.Validators;

/// <summary>Verifies the business rules enforced by <see cref="CsvTransactionRowValidator"/>.</summary>
public sealed class CsvTransactionRowValidatorTests : IAsyncDisposable
{
    private readonly TransactionFixture _fixture = new();
    private readonly CsvTransactionRowValidator _validator = new();

    /// <summary>
    /// <code>
    /// GIVEN a valid CSV transaction row
    ///  WHEN the validator evaluates it
    ///  THEN the result contains no validation errors
    /// </code>
    /// </summary>
    [Fact]
    public async Task Validate_ValidRow_HasNoErrors()
    {
        // Arrange
        var row = _fixture.CreateValidCsvRow();

        // Act
        var result = await _validator.ValidateAsync(row);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// <code>
    /// GIVEN a CSV transaction row with a zero amount
    ///  WHEN the validator evaluates it
    ///  THEN the result contains an error for Amount
    /// </code>
    /// </summary>
    [Fact]
    public async Task Validate_ZeroAmount_HasAmountError()
    {
        // Arrange
        var row = _fixture.CreateCsvRowWithAmount("0");

        // Act
        var result = await _validator.ValidateAsync(row);

        // Assert
        result.Errors.Should().Contain(error => error.PropertyName == nameof(row.Amount));
    }

    /// <summary>
    /// <code>
    /// GIVEN a CSV transaction row with a lowercase currency code
    ///  WHEN the validator evaluates it
    ///  THEN the result contains an error for Currency
    /// </code>
    /// </summary>
    [Fact]
    public async Task Validate_LowercaseCurrency_HasCurrencyError()
    {
        // Arrange
        var row = _fixture.CreateCsvRowWithCurrency("usd");

        // Act
        var result = await _validator.ValidateAsync(row);

        // Assert
        result.Errors.Should().Contain(error => error.PropertyName == nameof(row.Currency));
    }

    /// <summary>
    /// <code>
    /// GIVEN a CSV transaction row with a future transaction date
    ///  WHEN the validator evaluates it
    ///  THEN the result contains an error for TransactionDate
    /// </code>
    /// </summary>
    [Fact]
    public async Task Validate_FutureTransactionDate_HasTransactionDateError()
    {
        // Arrange
        var row = _fixture.CreateCsvRowWithTransactionDate(DateTimeOffset.UtcNow.AddSeconds(1).ToString("O"));

        // Act
        var result = await _validator.ValidateAsync(row);

        // Assert
        result.Errors.Should().Contain(error => error.PropertyName == nameof(row.TransactionDate));
    }

    /// <summary>
    /// <code>
    /// GIVEN a CSV transaction row with an unsupported source channel
    ///  WHEN the validator evaluates it
    ///  THEN the result contains an error for SourceChannel
    /// </code>
    /// </summary>
    [Fact]
    public async Task Validate_InvalidSourceChannel_HasSourceChannelError()
    {
        // Arrange
        var row = _fixture.CreateCsvRowWithSourceChannel("desk");

        // Act
        var result = await _validator.ValidateAsync(row);

        // Assert
        result.Errors.Should().Contain(error => error.PropertyName == nameof(row.SourceChannel));
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _fixture.DisposeAsync();
}
