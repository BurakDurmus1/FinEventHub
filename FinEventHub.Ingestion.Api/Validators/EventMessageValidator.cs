using FinEventHub.Contracts.Messages;
using FluentValidation;
using System.Text.RegularExpressions;

namespace FinEventHub.Ingestion.Api.Validators;

public sealed class EventMessageValidator : AbstractValidator<EventMessage>
{
    public EventMessageValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty();

        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Amount)
            .GreaterThan(0);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z]{3}$");

        RuleFor(x => x.OccurredAt)
            .NotEmpty();
    }
}