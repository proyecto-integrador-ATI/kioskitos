using System.Security.Claims;

namespace Kioskitos.API.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id of the authenticated user, taken from the JWT "sub" claim.</summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirst("sub")?.Value, out var userId) ? userId : null;
}
