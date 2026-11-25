using Estehlaky.Application.Common.Results;
using Microsoft.AspNetCore.Identity;

namespace Estehlaky.Infrastructure.Seed;

internal static class SeedHelpers
{
    public static Result ToResult(
        this IdentityResult identityResult,
        Error error)
    {
        ArgumentNullException.ThrowIfNull(identityResult);
        ArgumentNullException.ThrowIfNull(error);

        if (identityResult.Succeeded)
            return Result.Success();

        var identityErrors = identityResult.Errors
            .Select(e => Error.Failure(
                $"{error.Code}.{e.Code}",
                e.Description))
            .ToList();

        return Result.Failure(
            identityErrors.Count > 0
                ? identityErrors
                : [error]);
    }
}