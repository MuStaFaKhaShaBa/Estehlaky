
using Estehlaky.API.Contracts;
using Estehlaky.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace Estehlaky.API.Extensions;

public static class ResultHttpExtensions
{
    // Non-generic success response
    public static IActionResult ToActionResult(
        this Result result,
        ControllerBase c,
        string message = "Operation completed successfully.",
        int statusCode = StatusCodes.Status200OK)
    {
        if (!result.IsSuccess)
            return result.ToProblem(c.HttpContext);

        return c.StatusCode(
            statusCode,
            new ApiResponse(
                true,
                statusCode,
                message));
    }

    // Generic success response
    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        ControllerBase c,
        Func<T, IActionResult>? onSuccess = null,
        string message = "Operation completed successfully.",
        int statusCode = StatusCodes.Status200OK)
    {
        if (!result.IsSuccess)
            return result.ToProblem(c.HttpContext);

        if (onSuccess is not null)
            return onSuccess(result.Value);

        return c.StatusCode(
            statusCode,
            new ApiResponse<T>(
                true,
                statusCode,
                message,
                result.Value));
    }

    // Error type to HTTP status code
    public static int ToStatusCode(this ErrorType type) => type switch
    {
        ErrorType.Validation =>
            StatusCodes.Status400BadRequest,

        ErrorType.Unauthorized =>
            StatusCodes.Status401Unauthorized,

        ErrorType.Forbidden =>
            StatusCodes.Status403Forbidden,

        ErrorType.NotFound =>
            StatusCodes.Status404NotFound,

        ErrorType.Conflict =>
            StatusCodes.Status409Conflict,

        ErrorType.ConfirmationRequired =>
            StatusCodes.Status409Conflict,

        ErrorType.Custom =>
            StatusCodes.Status400BadRequest,

        _ => StatusCodes.Status500InternalServerError
    };

    // Problem response using ControllerBase
    public static IActionResult ToProblem(
        this Result result,
        ControllerBase c)
        => result.ToProblem(c.HttpContext);

    // Centralized problem response
    public static IActionResult ToProblem(
        this Result result,
        HttpContext context)
    {
        var traceId = context.TraceIdentifier;

        // Hard errors must be fixed first.
        var hard = result.Errors
            .Where(e => e.Type != ErrorType.ConfirmationRequired)
            .ToList();

        // Confirmation is requested only when no hard errors exist.
        if (hard.Count == 0)
        {
            return new ConflictObjectResult(new
            {
                code = "requiresConfirmation",
                statusCode = StatusCodes.Status409Conflict,
                reasons = result.Errors
                    .SelectMany(e => e.Reasons ?? [])
                    .Distinct()
                    .ToList(),
                traceId
            });
        }

        // First hard error determines the response status.
        var primary = hard[0];
        var status = primary.Type.ToStatusCode();

        var problem = new ProblemDetails
        {
            Status = status,
            Title = primary.Code,
            Detail = primary.CustomError ?? primary.Description,
            Instance = context.Request.Path
        };

        problem.Extensions["code"] = primary.Code;
        problem.Extensions["statusCode"] = status;
        problem.Extensions["traceId"] = traceId;

        if (primary.CustomError is not null)
            problem.Extensions["customError"] = primary.CustomError;

        var fields = hard
            .SelectMany(e => e.Fields ?? [])
            .ToList();

        if (fields.Count > 0)
            problem.Extensions["fieldErrors"] = fields;

        if (hard.Count > 1)
        {
            problem.Extensions["errors"] = hard
                .Select(e => new
                {
                    code = e.Code,
                    message = e.CustomError ?? e.Description
                })
                .ToList();
        }

        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
    }
}
