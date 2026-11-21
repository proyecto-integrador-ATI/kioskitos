using System.Security.Claims;

namespace Kioskitos.API.Controllers;

internal static class ClaimsPrincipalExtensions
{
    /// <summary>Reads the authenticated user's id from the JWT "sub" claim.</summary>
    public static bool TryGetUserId(this ClaimsPrincipal user, out Guid userId) =>
        Guid.TryParse(user.FindFirst("sub")?.Value, out userId);
}
