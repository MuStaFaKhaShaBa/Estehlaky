using Estehlaky.Application.Abstractions.Services;
using Estehlaky.Application.Common.Results;
using MediatR;

namespace Estehlaky.Application.Auth.ResetPassword;

public sealed class ResetPasswordHandler(IAuthService auth)
    : IRequestHandler<ResetPasswordCommand, Result>
{
    public Task<Result> Handle(ResetPasswordCommand c, CancellationToken ct) =>
        auth.ResetPasswordAsync(c.Email, c.Phone, c.Token, c.NewPassword, ct);
}
