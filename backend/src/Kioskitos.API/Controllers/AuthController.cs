using Kioskitos.API.Contracts;
using Kioskitos.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kioskitos.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(GoogleLoginService googleLogin) : ControllerBase
{
    [HttpPost("google")]
    public async Task<ActionResult<AuthResult>> LoginWithGoogle(
        GoogleLoginRequest request, CancellationToken ct)
    {
        var result = await googleLogin.LoginAsync(request.IdToken, ct);
        return result is null ? Unauthorized() : Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        UserId = User.FindFirst("sub")?.Value,
        Email = User.FindFirst("email")?.Value,
        Name = User.FindFirst("name")?.Value
    });
}