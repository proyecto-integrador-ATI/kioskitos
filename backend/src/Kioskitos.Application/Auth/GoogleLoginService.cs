using Kioskitos.Domain.Entities;

namespace Kioskitos.Application.Auth;

public sealed record AuthResult(
    string AccessToken, Guid UserId, string Email, string Name, bool IsNewUser);

public sealed class GoogleLoginService(
    IGoogleTokenValidator googleValidator,
    IUserRepository users,
    IJwtTokenGenerator jwtGenerator)
{
    public async Task<AuthResult?> LoginAsync(string idToken, CancellationToken ct)
    {
        var googleUser = await googleValidator.ValidateAsync(idToken, ct);
        if (googleUser is null)
        {
            return null;
        }

        var user = await users.GetByGoogleSubjectAsync(googleUser.Subject, ct);
        var isNewUser = user is null;

        if (user is null) // RF 01.04: registro automático
        {
            user = new User(googleUser.Subject, googleUser.Email, googleUser.Name);
            await users.AddAsync(user, ct);
            await users.SaveChangesAsync(ct);
        }

        var token = jwtGenerator.Generate(user);
        return new AuthResult(token, user.Id, user.Email, user.Name, isNewUser);
    }
}