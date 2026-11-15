using Google.Apis.Auth;
using Kioskitos.Application.Auth;
using Microsoft.Extensions.Options;

namespace Kioskitos.Infrastructure.Auth;

public sealed class GoogleTokenValidator(IOptions<GoogleOptions> options) : IGoogleTokenValidator
{
    public async Task<GoogleUser?> ValidateAsync(string idToken, CancellationToken ct)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [options.Value.ClientId]
                });

            if (!payload.EmailVerified)
            {
                return null;
            }

            return new GoogleUser(payload.Subject, payload.Email, payload.Name ?? payload.Email);
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}