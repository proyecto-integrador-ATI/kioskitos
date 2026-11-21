using Kioskitos.API.Auth;
using Kioskitos.API.Contracts;
using Kioskitos.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kioskitos.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users/me")]
public sealed class UsersController(UserProfileService profiles) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserProfileDto>> Get(CancellationToken ct)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var profile = await profiles.GetAsync(userId, ct);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut]
    public async Task<ActionResult<UserProfileDto>> Update(
        UpdateUserProfileRequest request, CancellationToken ct)
    {
        // The user is always the authenticated one: identity never comes from the request body.
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var command = new UpdateUserProfileCommand(request.Name, request.Industry, request.HourlyRate);
        var profile = await profiles.UpdateAsync(userId, command, ct);

        return profile is null ? NotFound() : Ok(profile);
    }
}
