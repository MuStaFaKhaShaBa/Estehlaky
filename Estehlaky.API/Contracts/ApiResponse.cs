namespace Estehlaky.API.Contracts;

public record ApiResponse(
    bool Success,
    int StatusCode,
    string Message);

public sealed record ApiResponse<T>(
    bool Success,
    int StatusCode,
    string Message,
    T? Data)
    : ApiResponse(Success, StatusCode, Message);