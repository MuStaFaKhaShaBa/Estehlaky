using Estehlaky.Application.Common.Results;
using MediatR;

namespace Estehlaky.Application.Me.Get;

public sealed record GetMeQuery : IRequest<Result<UserDto>>;
