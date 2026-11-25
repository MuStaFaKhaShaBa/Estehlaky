using Estehlaky.Application.Abstractions.Services;
using Estehlaky.Application.Common.Results;
using MediatR;

namespace Estehlaky.Application.Me.Get;

public sealed class GetMeHandler(IAuthService auth, ICurrentUser current)
    : IRequestHandler<GetMeQuery, Result<UserDto>>
{
    public Task<Result<UserDto>> Handle(GetMeQuery q, CancellationToken ct) =>
        current.Id is Guid id
            ? auth.GetUserAsync(id, ct)
            : Task.FromResult<Result<UserDto>>(Error.Unauthorized());
}
