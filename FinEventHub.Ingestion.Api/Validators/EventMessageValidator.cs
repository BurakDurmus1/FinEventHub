using FinEventHub.Contracts.Messages;
using FluentValidation;

namespace FinEventHub.Ingestion.Api.Validators;

public sealed class EventMessageValidator : AbstractValidator<EventMessage>
{
    public EventMessageValidator()
    {

    }
}