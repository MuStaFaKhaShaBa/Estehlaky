using Estehlaky.API.Extensions;
using Estehlaky.Application.Me.Get;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Estehlaky.API.Controllers
{
    /// <summary>
    /// Provides access to the currently authenticated user's profile.
    /// </summary>
    /// <remarks>
    /// Requires a valid JWT access token.
    /// The user identity is resolved from the authenticated claims.
    /// Clients should not provide a user id in the request.
    /// </remarks>
    [ApiController]
    [Authorize]
    [Route("api/v1/me")]
    [Produces("application/json")]
    public class MeController(ISender sender) : ControllerBase
    {
        /// <summary>
        /// Gets the profile of the currently authenticated user.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <response code="200">Returns the current user's profile.</response>
        /// <response code="401">Missing, expired, or invalid access token.</response>
        /// <response code="404">The authenticated user's profile was not found.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(CancellationToken ct) =>
            (await sender.Send(new GetMeQuery(), ct))
                .ToActionResult(this);
    }
}
