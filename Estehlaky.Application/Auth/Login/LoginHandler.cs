using Estehlaky.Application.Abstractions.Services;
using Estehlaky.Application.Common.Results;
using MediatR;

namespace Estehlaky.Application.Auth.Login;

public sealed class LoginHandler(IAuthService auth)
    : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    public Task<Result<AuthResponseDto>> Handle(LoginCommand c, CancellationToken ct) =>
        auth.LoginAsync(c.Email,c.Phone, c.Password, c.Client, ct);
}
