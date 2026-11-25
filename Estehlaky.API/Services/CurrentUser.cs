using System.Security.Claims;
using Estehlaky.Application.Abstractions.Services;

namespace Estehlaky.API.Services;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    // Inbound claim mapping is turned off, so the user id is the raw "sub" claim.
    public Guid? Id =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirstValue("sub"), out var id) ? id : null;

    public bool IsAuthenticated =>
        accessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
