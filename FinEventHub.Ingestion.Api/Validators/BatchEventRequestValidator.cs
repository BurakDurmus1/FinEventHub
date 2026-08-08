using FinEventHub.Contracts.Requests;
using FluentValidation;

namespace FinEventHub.Ingestion.Api.Validators;

public sealed class BatchEventRequestValidator : AbstractValidator<BatchEventRequest>
{
    public BatchEventRequestValidator()
    {
        RuleFor(x => x.Events)
            .NotEmpty()
            .Must(events => events.Count <= 1000)
            .WithMessage("Maximum 1000 events are allowed.");

        RuleFor(x => x.Events)
            .Must(events => events.Select(e => e.EventId).Distinct().Count() == events.Count)
            .WithMessage("Duplicate EventId values are not allowed.");

        RuleForEach(x => x.Events)
            .SetValidator(new EventMessageValidator());
    }
}