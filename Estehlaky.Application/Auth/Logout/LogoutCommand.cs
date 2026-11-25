using Estehlaky.Application.Common.Results;
using MediatR;

namespace Estehlaky.Application.Auth.Logout;

/// <summary>Ends the session (token family) that this refresh token belongs to.</summary>
public sealed record LogoutCommand(string RefreshToken) : IRequest<Result>;
