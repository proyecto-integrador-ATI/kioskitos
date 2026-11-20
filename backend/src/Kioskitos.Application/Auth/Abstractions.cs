using Kioskitos.Domain.Entities;

namespace Kioskitos.Application.Auth;

public sealed record GoogleUser(string Subject, string Email, string Name);

public interface IGoogleTokenValidator
{
    Task<GoogleUser?> ValidateAsync(string idToken, CancellationToken ct);
}

public interface IJwtTokenGenerator
{
    string Generate(User user);
}