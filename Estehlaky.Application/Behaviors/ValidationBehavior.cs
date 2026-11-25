using Estehlaky.Application.Common.Results;
using FluentValidation;
using MediatR;

namespace Estehlaky.Application.Behaviors;

public class ValidationBehavior<TReq, TRes>(IEnumerable<IValidator<TReq>> validators)
    : IPipelineBehavior<TReq, TRes>
    where TReq : notnull
    where TRes : IResult<TRes>
{
    public async Task<TRes> Handle(TReq request, RequestHandlerDelegate<TRes> next, CancellationToken ct)
    {
        if (!validators.Any()) return await next(ct);

        var ctx = new ValidationContext<TReq>(request);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(ctx, ct)));
        var failures = results.SelectMany(r => r.Errors).ToList();

        return failures.Count > 0
            ? TRes.CreateFailure(failures.ToError())
            : await next(ct);
    }
}
