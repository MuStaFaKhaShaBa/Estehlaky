using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Estehlaky.API.Middleware;

/// <summary>Only for unexpected failures. Expected outcomes travel as Result, never as exceptions.</summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        logger.LogError(exception, "Unhandled exception. TraceId {TraceId}", context.TraceIdentifier);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // Never expose exception text or stack traces to the client.
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "General.Unexpected",
                Detail = "An unexpected error occurred.",
                Extensions =
                {
                    ["code"] = "General.Unexpected",
                    ["traceId"] = context.TraceIdentifier
                }
            }
        });
    }
}
