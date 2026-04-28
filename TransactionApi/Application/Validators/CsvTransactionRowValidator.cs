using System.Globalization;
using FluentValidation;
using TransactionApi.Application.DTOs;

namespace TransactionApi.Application.Validators;

/// <summary>
/// Validates a CSV transaction row before it is transformed into a domain transaction.
/// </summary>
public sealed class CsvTransactionRowValidator : AbstractValidator<CsvTransactionRow>
{
    /// <summary>
    /// Initializes the validator with all supported CSV field rules.
    /// </summary>
    public CsvTransactionRowValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.TransactionId)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.TransactionDate)
            .NotEmpty()
            .Must(TryParseTransactionDate)
            .WithMessage("Transaction date must be a valid timestamp.")
            .Must(NotBeInFuture)
            .WithMessage("Transaction date cannot be in the future.")
            .Must(NotBeOlderThanTenYears)
            .WithMessage("Transaction date cannot be older than 10 years.");

        RuleFor(x => x.Amount)
            .NotEmpty()
            .Must(TryParseAmount)
            .WithMessage("Amount must be a valid decimal value.")
            .Must(BeGreaterThanZero)
            .WithMessage("Amount must be greater than 0.")
            .Must(BeLessThanOneBillion)
            .WithMessage("Amount must be less than 1000000000.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z]{3}$");

        RuleFor(x => x.SourceChannel)
            .NotEmpty()
            .Must(static channel => AllowedChannels.Contains(channel))
            .WithMessage("Source channel must be one of: web, mobile, pos, api, import.");
    }

    private static readonly HashSet<string> AllowedChannels =
    [
        "web",
        "mobile",
        "pos",
        "api",
        "import"
    ];

    private static bool TryParseTransactionDate(string value)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _);

    private static bool NotBeInFuture(string value)
        => TryGetTransactionDate(value, out var parsed) && parsed <= DateTimeOffset.UtcNow;

    private static bool NotBeOlderThanTenYears(string value)
        => TryGetTransactionDate(value, out var parsed) && parsed >= DateTimeOffset.UtcNow.AddYears(-10);

    private static bool TryParseAmount(string value)
        => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _);

    private static bool BeGreaterThanZero(string value)
        => TryGetAmount(value, out var parsed) && parsed > 0m;

    private static bool BeLessThanOneBillion(string value)
        => TryGetAmount(value, out var parsed) && parsed < 1_000_000_000m;

    private static bool TryGetTransactionDate(string value, out DateTimeOffset parsed)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out parsed);

    private static bool TryGetAmount(string value, out decimal parsed)
        => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed);
}
