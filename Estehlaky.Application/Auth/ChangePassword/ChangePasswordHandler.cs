using Estehlaky.Application.Abstractions.Services;
using Estehlaky.Application.Common.Results;
using MediatR;

namespace Estehlaky.Application.Auth.ChangePassword;

public sealed class ChangePasswordHandler(IAuthService auth, ICurrentUser current)
    : IRequestHandler<ChangePasswordCommand, Result>
{
    public Task<Result> Handle(ChangePasswordCommand c, CancellationToken ct) =>
        current.Id is Guid id
            ? auth.ChangePasswordAsync(id, c.CurrentPassword, c.NewPassword, ct)
            : Task.FromResult<Result>(Error.Unauthorized());
}
