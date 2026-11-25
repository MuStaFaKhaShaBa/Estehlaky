namespace Estehlaky.Application.Common.Results;

/// <summary>One single error. A Result may carry many of these.</summary>
public sealed record Error(string Code, string Description, ErrorType Type)
{
    /// <summary>Free-text message supplied by the caller (only set for Custom errors).</summary>
    public string? CustomError { get; init; }

    /// <summary>Per-field details (Validation errors).</summary>
    public IReadOnlyList<FieldError>? Fields { get; init; }

    /// <summary>Reasons the user must confirm (ConfirmationRequired errors).</summary>
    public IReadOnlyList<string>? Reasons { get; init; }

    public static Error Validation(IReadOnlyList<FieldError> fields) =>
        new("General.Validation", "One or more validation errors occurred.", ErrorType.Validation)
        { Fields = fields };

    public static Error NotFound(
        string code = "General.NotFound",
        string description = "Resource not found.") =>
        new(code, description, ErrorType.NotFound);

    public static Error Forbidden(string code = "General.Forbidden") =>
        new(code, "Access denied.", ErrorType.Forbidden);

    public static Error Unauthorized(string code = "General.Unauthorized") =>
        new(code, "Authentication required.", ErrorType.Unauthorized);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    public static Error Confirmation(IReadOnlyList<string> reasons) =>
        new("General.RequiresConfirmation", "Confirmation required.", ErrorType.ConfirmationRequired)
        { Reasons = reasons };

    public static Error Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);

    /// <summary>One-off error with any message you want. Pass a code if the client must react to it.</summary>
    public static Error Custom(string message, string code = "General.Custom") =>
        new(code, message, ErrorType.Custom) { CustomError = message };
}
