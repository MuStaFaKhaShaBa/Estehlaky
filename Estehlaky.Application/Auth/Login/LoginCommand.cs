using Estehlaky.Application.Abstractions.AppSettingsModels;
using Estehlaky.Application.Common.Results;
using MediatR;

namespace Estehlaky.Application.Auth.Login;

public sealed record LoginCommand(string Email, string? Phone, string Password, ClientInfo Client)
    : IRequest<Result<AuthResponseDto>>;
