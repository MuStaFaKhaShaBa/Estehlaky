namespace Estehlaky.Application.Common.Results;

public enum ErrorType
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    ConfirmationRequired,
    Custom,
    Failure,

    Unavailable
}
