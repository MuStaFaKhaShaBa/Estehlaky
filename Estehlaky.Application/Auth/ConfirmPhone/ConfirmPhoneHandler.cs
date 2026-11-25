using Estehlaky.Application.Abstractions.Services;
using Estehlaky.Application.Common.Results;
using MediatR;

namespace Estehlaky.Application.Auth.ConfirmPhone;

public sealed class RequestPhoneConfirmationHandler(IAuthService auth, ICurrentUser current)
    : IRequestHandler<RequestPhoneConfirmationCommand, Result>
{
    public Task<Result> Handle(RequestPhoneConfirmationCommand c, CancellationToken ct) =>
        current.IsAuthenticated && current.Id is Guid id
            ? auth.RequestPhoneConfirmationAsync(id, ct)
            : Task.FromResult<Result>(Error.Unauthorized());
}

public sealed class ConfirmPhoneHandler(IAuthService auth, ICurrentUser current)
    : IRequestHandler<ConfirmPhoneCommand, Result>
{
    public Task<Result> Handle(ConfirmPhoneCommand c, CancellationToken ct) =>
        current.IsAuthenticated && current.Id is Guid id
            ? auth.ConfirmPhoneAsync(id, c.Otp, ct)
            : Task.FromResult<Result>(Error.Unauthorized());
}
