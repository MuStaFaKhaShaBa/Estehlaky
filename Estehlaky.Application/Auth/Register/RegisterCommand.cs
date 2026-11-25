using Estehlaky.Application.Abstractions.AppSettingsModels;
using Estehlaky.Application.Common.Results;
using MediatR;

namespace Estehlaky.Application.Auth.Register;

public sealed record RegisterCommand(
    string Email, string Password, string? FullName, string? Phone, string Language, ClientInfo Client)
    : IRequest<Result<AuthResponseDto>>;
