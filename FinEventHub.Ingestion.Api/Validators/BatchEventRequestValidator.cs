using FinEventHub.Contracts.Requests;
using FluentValidation;

namespace FinEventHub.Ingestion.Api.Validators;

public sealed class BatchEventRequestValidator : AbstractValidator<BatchEventRequest>
{
    public BatchEventRequestValidator()
    {

    }
}