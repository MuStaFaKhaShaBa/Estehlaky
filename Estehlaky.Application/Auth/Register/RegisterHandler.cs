using Estehlaky.Application.Abstractions.Services;
using Estehlaky.Application.Common.Results;
using MediatR;

namespace Estehlaky.Application.Auth.Register;

public sealed class RegisterHandler(IAuthService auth)
    : IRequestHandler<RegisterCommand, Result<AuthResponseDto>>
{
    public Task<Result<AuthResponseDto>> Handle(RegisterCommand c, CancellationToken ct) =>
        auth.RegisterAsync(c.Email, c.Password, c.FullName, c.Phone, c.Language, c.Client, ct);
}
