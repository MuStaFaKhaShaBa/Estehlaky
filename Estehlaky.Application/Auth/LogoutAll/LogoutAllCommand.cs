using Estehlaky.Application.Common.Results;
using MediatR;

namespace Estehlaky.Application.Auth.LogoutAll;

public sealed record LogoutAllCommand : IRequest<Result>;
