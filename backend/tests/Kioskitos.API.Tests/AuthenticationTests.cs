using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Kioskitos.Application.Auth;
using Kioskitos.Domain.Entities;
using Kioskitos.Infrastructure.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Kioskitos.API.Tests;

public sealed class AuthenticationTests
{
    [Fact]
    public async Task LoginWithGoogle_CreatesNewUserAndReturnsJwt()
    {
        var users = new FakeUserRepository();
        var service = new GoogleLoginService(
            new FakeGoogleTokenValidator(
                new GoogleUser("google-subject", "user@example.com", "Test User")),
            users,
            new FakeJwtTokenGenerator("jwt-token"));

        var result = await service.LoginAsync("valid-id-token", CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result!.IsNewUser);
        Assert.Equal("jwt-token", result.AccessToken);
        Assert.Single(users.AddedUsers);
        Assert.Equal("user@example.com", users.AddedUsers[0].Email);
    }

    [Fact]
    public async Task LoginWithGoogle_ExistingUserDoesNotCreateDuplicate()
    {
        var existingUser = new User("google-subject", "user@example.com", "Test User");
        var users = new FakeUserRepository(existingUser);
        var service = new GoogleLoginService(
            new FakeGoogleTokenValidator(
                new GoogleUser("google-subject", "user@example.com", "Test User")),
            users,
            new FakeJwtTokenGenerator("jwt-token"));

        var result = await service.LoginAsync("valid-id-token", CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.IsNewUser);
        Assert.Empty(users.AddedUsers);
    }

    [Fact]
    public async Task LoginWithGoogle_InvalidTokenReturnsNull()
    {
        var users = new FakeUserRepository();
        var service = new GoogleLoginService(
            new FakeGoogleTokenValidator(null),
            users,
            new FakeJwtTokenGenerator("jwt-token"));

        var result = await service.LoginAsync("invalid-id-token", CancellationToken.None);

        Assert.Null(result);
        Assert.Empty(users.AddedUsers);
    }

    [Fact]
    public void GeneratedJwt_ContainsUserClaimsAndValidatesIssuerAudienceAndLifetime()
    {
        var options = Options.Create(new JwtOptions
        {
            Key = "test-jwt-key-with-at-least-32-characters",
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpirationMinutes = 5
        });
        var user = new User("google-subject", "user@example.com", "Test User");
        var token = new JwtTokenGenerator(options).Generate(user);
        var handler = new JwtSecurityTokenHandler();
        handler.MapInboundClaims = false;

        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "test-issuer",
            ValidateAudience = true,
            ValidAudience = "test-audience",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(options.Value.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        }, out _);

        Assert.Equal(user.Id.ToString(), principal.FindFirst("sub")?.Value);
        Assert.Equal(user.Email, principal.FindFirst("email")?.Value);
        Assert.Equal(user.Name, principal.FindFirst("name")?.Value);
    }

    [Fact]
    public void GeneratedJwt_WithWrongIssuerIsRejected()
    {
        var token = CreateJwt(
            issuer: "unexpected-issuer",
            audience: "test-audience",
            expires: DateTime.UtcNow.AddMinutes(5));

        Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
            ValidateJwt(token));
    }

    [Fact]
    public void GeneratedJwt_WithWrongAudienceIsRejected()
    {
        var token = CreateJwt(
            issuer: "test-issuer",
            audience: "unexpected-audience",
            expires: DateTime.UtcNow.AddMinutes(5));

        Assert.Throws<SecurityTokenInvalidAudienceException>(() =>
            ValidateJwt(token));
    }

    [Fact]
    public void GeneratedJwt_WhenExpiredIsRejected()
    {
        var token = CreateJwt(
            issuer: "test-issuer",
            audience: "test-audience",
            expires: DateTime.UtcNow.AddMinutes(-5));

        Assert.ThrowsAny<SecurityTokenException>(() =>
            ValidateJwt(token));
    }

    private static string CreateJwt(
        string issuer,
        string audience,
        DateTime expires)
    {
        var user = new User("google-subject", "user@example.com", "Test User");
        var options = Options.Create(new JwtOptions
        {
            Key = "test-jwt-key-with-at-least-32-characters",
            Issuer = issuer,
            Audience = audience,
            ExpirationMinutes = expires > DateTime.UtcNow ? 5 : -5
        });

        return new JwtTokenGenerator(options).Generate(user);
    }

    private static ClaimsPrincipal ValidateJwt(string token)
    {
        var handler = new JwtSecurityTokenHandler
        {
            MapInboundClaims = false
        };

        return handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "test-issuer",
            ValidateAudience = true,
            ValidAudience = "test-audience",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("test-jwt-key-with-at-least-32-characters")),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        }, out _);
    }

    private sealed class FakeGoogleTokenValidator(GoogleUser? result)
        : IGoogleTokenValidator
    {
        public Task<GoogleUser?> ValidateAsync(
            string idToken,
            CancellationToken ct) => Task.FromResult(result);
    }

    private sealed class FakeJwtTokenGenerator(string token)
        : IJwtTokenGenerator
    {
        public string Generate(User user) => token;
    }

    private sealed class FakeUserRepository(User? existingUser = null)
        : IUserRepository
    {
        public List<User> AddedUsers { get; } = [];

        public Task<User?> GetByGoogleSubjectAsync(
            string googleSubject,
            CancellationToken ct) =>
            Task.FromResult(existingUser);

        public Task AddAsync(User user, CancellationToken ct)
        {
            AddedUsers.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct) =>
            Task.CompletedTask;
    }
}
