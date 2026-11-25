namespace Estehlaky.Application.Common.Results;

public sealed record FieldError(string Field, string Code, string Message);
