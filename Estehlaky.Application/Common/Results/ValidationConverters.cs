using System.Text.Json;
using FluentValidation.Results;

namespace Estehlaky.Application.Common.Results;

public static class ValidationConverters
{
    public static Error ToError(this ValidationResult result) =>
        result.Errors.ToError();

    public static Error ToError(this IEnumerable<ValidationFailure> failures) =>
        Error.Validation(failures
            .Where(f => f is not null)
            .Select(f => new FieldError(
                JsonNamingPolicy.CamelCase.ConvertName(f.PropertyName),
                f.ErrorCode,
                f.ErrorMessage))
            .ToList());
}
