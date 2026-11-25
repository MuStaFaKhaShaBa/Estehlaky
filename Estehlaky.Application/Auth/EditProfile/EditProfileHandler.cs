using Estehlaky.Application.Abstractions.Services;
using Estehlaky.Application.Common.Results;
using Estehlaky.Application.Me;
using MediatR;

namespace Estehlaky.Application.Auth.EditProfile;

public sealed class EditProfileHandler(IAuthService auth, ICurrentUser current)
    : IRequestHandler<EditProfileCommand, Result<UserDto>>
{
    public Task<Result<UserDto>> Handle(EditProfileCommand c, CancellationToken ct) => 
        current.IsAuthenticated && current.Id is Guid id
            ? auth.EditProfileAsync(id, c.FullName, c.PhoneNumber, c.ProfilePictureUrl, c.Language, c.Client, ct)
            : Task.FromResult<Result<UserDto>>(Error.Unauthorized());
}
