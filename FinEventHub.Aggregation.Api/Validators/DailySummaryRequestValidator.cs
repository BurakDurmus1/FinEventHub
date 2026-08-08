using FluentValidation;

namespace FinEventHub.Aggregation.Api.Validators;

public class GetDailySummaryRequest
{
    public string CustomerId { get; set; } = default!;
    public DateOnly Date { get; set; }
    public string Currency { get; set; } = default!;
}

public class GetDailySummaryRequestValidator : AbstractValidator<GetDailySummaryRequest>
{
    public GetDailySummaryRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Currency)
            .Matches("^[A-Z]{3}$")
            .WithMessage("Currency must be a 3-letter uppercase ISO code.");
    }
}