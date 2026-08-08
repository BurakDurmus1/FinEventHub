using FinEventHub.Aggregation.Api.Models;
using FluentValidation;

namespace FinEventHub.Aggregation.Api.Validators;

public sealed class DailySummaryQueryValidator : AbstractValidator<DailySummaryQuery>
{
    public DailySummaryQueryValidator()
    {
        RuleFor(x => x.Currency)
     .Cascade(CascadeMode.Stop)
     .NotEmpty()
     .Length(3)
     .Matches("^[A-Z]{3}$")
     .WithMessage("Currency must be a valid 3-letter uppercase ISO currency code.");
    }
}