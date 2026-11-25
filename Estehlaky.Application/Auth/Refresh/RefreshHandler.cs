using Estehlaky.Application.Abstractions.Services;
using Estehlaky.Application.Common.Results;
using MediatR;

namespace Estehlaky.Application.Auth.Refresh;

public sealed class RefreshHandler(IAuthService auth)
    : IRequestHandler<RefreshCommand, Result<AuthResponseDto>>
{
    public Task<Result<AuthResponseDto>> Handle(RefreshCommand c, CancellationToken ct) =>
        auth.RefreshAsync(c.RefreshToken, c.Client, ct);
}
