using Estehlaky.Application.Abstractions.Services;
using Estehlaky.Application.Common.Results;
using MediatR;

namespace Estehlaky.Application.Auth.ConfirmEmail;

public sealed class RequestEmailConfirmationHandler(IAuthService auth, ICurrentUser current)
    : IRequestHandler<RequestEmailConfirmationCommand, Result>
{
    public Task<Result> Handle(RequestEmailConfirmationCommand c, CancellationToken ct) =>
        current.IsAuthenticated && current.Id is Guid id
            ? auth.RequestEmailConfirmationAsync(id, ct)
            : Task.FromResult<Result>(Error.Unauthorized());
}

public sealed class ConfirmEmailHandler(IAuthService auth, ICurrentUser current)
    : IRequestHandler<ConfirmEmailCommand, Result>
{
    public Task<Result> Handle(ConfirmEmailCommand c, CancellationToken ct) =>
        current.IsAuthenticated && current.Id is Guid id
            ? auth.ConfirmEmailAsync(id, c.Otp, ct)
            : Task.FromResult<Result>(Error.Unauthorized());
}
