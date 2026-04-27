using FluentValidation;
using TransactionApi.Application.DTOs;

namespace TransactionApi.Application.Validators;

/// <summary>Validates a real-time transaction ingestion request.</summary>
public sealed class TransactionInputDtoValidator : AbstractValidator<TransactionInputDto>
{
    /// <summary>Initialises the validator with all supported transaction field rules.</summary>
    public TransactionInputDtoValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.TransactionId)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.TransactionDate)
            .Must(static value => value != default)
            .WithMessage("Transaction date is required.")
            .Must(static value => value <= DateTimeOffset.UtcNow.AddMinutes(1))
            .WithMessage("Transaction date cannot be in the future.")
            .Must(static value => value >= DateTimeOffset.UtcNow.AddYears(-10))
            .WithMessage("Transaction date cannot be older than 10 years.");

        RuleFor(x => x.Amount)
            .GreaterThan(0m)
            .LessThan(1_000_000_000m);

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
}
