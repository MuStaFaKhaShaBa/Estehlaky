using Estehlaky.Application.Abstractions.AppSettingsModels;
using Estehlaky.Application.Common.Results;
using MediatR;

namespace Estehlaky.Application.Auth.Refresh;

public sealed record RefreshCommand(string RefreshToken, ClientInfo Client)
    : IRequest<Result<AuthResponseDto>>;
