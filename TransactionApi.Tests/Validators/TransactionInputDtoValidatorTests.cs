using TransactionApi.Application.Validators;
using TransactionApi.Tests.Fixtures;

namespace TransactionApi.Tests.Validators;

/// <summary>Verifies the business rules enforced by <see cref="TransactionInputDtoValidator"/>.</summary>
public sealed class TransactionInputDtoValidatorTests : IAsyncDisposable
{
    private readonly TransactionFixture _fixture = new();
    private readonly TransactionInputDtoValidator _validator = new();

    /// <summary>
    /// <code>
    /// GIVEN a valid transaction DTO
    ///  WHEN the validator evaluates it
    ///  THEN the result contains no validation errors
    /// </code>
    /// </summary>
    [Fact]
    public async Task Validate_ValidDto_HasNoErrors()
    {
        // Arrange
        var dto = _fixture.CreateValidDto();

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// <code>
    /// GIVEN a transaction DTO with an empty customer identifier
    ///  WHEN the validator evaluates it
    ///  THEN the result contains an error for CustomerId
    /// </code>
    /// </summary>
    [Fact]
    public async Task Validate_EmptyCustomerId_HasCustomerIdError()
    {
        // Arrange
        var dto = _fixture.CreateDtoWithEmptyCustomerId();

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.Errors.Should().Contain(error => error.PropertyName == nameof(dto.CustomerId));
    }

    /// <summary>
    /// <code>
    /// GIVEN a transaction DTO with a zero amount
    ///  WHEN the validator evaluates it
    ///  THEN the result contains an error for Amount
    /// </code>
    /// </summary>
    [Fact]
    public async Task Validate_ZeroAmount_HasAmountError()
    {
        // Arrange
        var dto = _fixture.CreateDtoWithAmount(0m);

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.Errors.Should().Contain(error => error.PropertyName == nameof(dto.Amount));
    }

    /// <summary>
    /// <code>
    /// GIVEN a transaction DTO with a negative amount
    ///  WHEN the validator evaluates it
    ///  THEN the result contains an error for Amount
    /// </code>
    /// </summary>
    [Fact]
    public async Task Validate_NegativeAmount_HasAmountError()
    {
        // Arrange
        var dto = _fixture.CreateDtoWithAmount(-1m);

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.Errors.Should().Contain(error => error.PropertyName == nameof(dto.Amount));
    }

    /// <summary>
    /// <code>
    /// GIVEN a transaction DTO with a lowercase currency code
    ///  WHEN the validator evaluates it
    ///  THEN the result contains an error for Currency
    /// </code>
    /// </summary>
    [Fact]
    public async Task Validate_LowercaseCurrency_HasCurrencyError()
    {
        // Arrange
        var dto = _fixture.CreateDtoWithCurrency("usd");

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.Errors.Should().Contain(error => error.PropertyName == nameof(dto.Currency));
    }

    /// <summary>
    /// <code>
    /// GIVEN a transaction DTO with an invalid currency length
    ///  WHEN the validator evaluates it
    ///  THEN the result contains an error for Currency
    /// </code>
    /// </summary>
    [Fact]
    public async Task Validate_InvalidCurrencyLength_HasCurrencyError()
    {
        // Arrange
        var dto = _fixture.CreateDtoWithCurrency("USDD");

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.Errors.Should().Contain(error => error.PropertyName == nameof(dto.Currency));
    }

    /// <summary>
    /// <code>
    /// GIVEN a transaction DTO with a future transaction date
    ///  WHEN the validator evaluates it
    ///  THEN the result contains an error for TransactionDate
    /// </code>
    /// </summary>
    [Fact]
    public async Task Validate_FutureTransactionDate_HasTransactionDateError()
    {
        // Arrange
        var dto = _fixture.CreateDtoWithTransactionDate(DateTimeOffset.UtcNow.AddSeconds(1));

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.Errors.Should().Contain(error => error.PropertyName == nameof(dto.TransactionDate));
    }

    /// <summary>
    /// <code>
    /// GIVEN a transaction DTO older than ten years
    ///  WHEN the validator evaluates it
    ///  THEN the result contains an error for TransactionDate
    /// </code>
    /// </summary>
    [Fact]
    public async Task Validate_TooOldTransactionDate_HasTransactionDateError()
    {
        // Arrange
        var dto = _fixture.CreateDtoWithTransactionDate(DateTimeOffset.UtcNow.AddYears(-11));

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.Errors.Should().Contain(error => error.PropertyName == nameof(dto.TransactionDate));
    }

    /// <summary>
    /// <code>
    /// GIVEN a transaction DTO with an unsupported source channel
    ///  WHEN the validator evaluates it
    ///  THEN the result contains an error for SourceChannel
    /// </code>
    /// </summary>
    [Fact]
    public async Task Validate_InvalidSourceChannel_HasSourceChannelError()
    {
        // Arrange
        var dto = _fixture.CreateDtoWithSourceChannel("desk");

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.Errors.Should().Contain(error => error.PropertyName == nameof(dto.SourceChannel));
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _fixture.DisposeAsync();
}
