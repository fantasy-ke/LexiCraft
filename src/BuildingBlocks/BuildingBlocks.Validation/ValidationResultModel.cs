using FluentValidation.Results;
using BuildingBlocks.Extensions.System;

namespace BuildingBlocks.Validation;

public class ValidationResultModel<TRequest>
{
    public ValidationResultModel(ValidationResult? validationResult)
    {
        var validationError = $"Validation failed for type {typeof(TRequest).Name}";
        Errors = validationResult
            ?.Errors.Select(error => new ValidationError(error.PropertyName, error.ErrorMessage))
            .ToList();

        Message = new { Message = validationError, Errors }.ToJson();
    }

    public string Message { get; set; }

    public IList<ValidationError>? Errors { get; }
}