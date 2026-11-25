using Estehlaky.Application.Abstractions.Services;
using Estehlaky.Application.Common.Results;
using MediatR;

namespace Estehlaky.Application.Auth.LogoutAll;

public sealed class LogoutAllHandler(IAuthService auth, ICurrentUser current)
    : IRequestHandler<LogoutAllCommand, Result>
{
    public Task<Result> Handle(LogoutAllCommand c, CancellationToken ct) =>
        current.Id is Guid id
            ? auth.LogoutAllAsync(id, ct)
            : Task.FromResult<Result>(Error.Unauthorized());
}
