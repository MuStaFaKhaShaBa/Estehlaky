using Estehlaky.Application.Abstractions.AppSettingsModels;
using Estehlaky.Application.Common.Results;
using Estehlaky.Application.Me;
using MediatR;

namespace Estehlaky.Application.Auth.EditProfile;

public sealed record EditProfileCommand(
    string? FullName,
    string? PhoneNumber,
    string? ProfilePictureUrl,
    string? Language,
    ClientInfo Client)
    : IRequest<Result<UserDto>>;