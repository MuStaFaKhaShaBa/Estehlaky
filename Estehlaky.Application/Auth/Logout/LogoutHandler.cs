using Estehlaky.Application.Abstractions.Services;
using Estehlaky.Application.Common.Results;
using MediatR;

namespace Estehlaky.Application.Auth.Logout;

public sealed class LogoutHandler(IAuthService auth) : IRequestHandler<LogoutCommand, Result>
{
    public Task<Result> Handle(LogoutCommand c, CancellationToken ct) =>
        auth.LogoutAsync(c.RefreshToken, ct);
}
