using Estehlaky.Application.Abstractions.Services;
using Estehlaky.Application.Common.Results;
using MediatR;

namespace Estehlaky.Application.Auth.ForgotPassword;

public sealed class ForgotPasswordHandler(IAuthService auth)
    : IRequestHandler<ForgotPasswordCommand, Result>
{
    public Task<Result> Handle(ForgotPasswordCommand c, CancellationToken ct) =>
        auth.ForgotPasswordAsync(c.Email, c.Phone, ct);
}
