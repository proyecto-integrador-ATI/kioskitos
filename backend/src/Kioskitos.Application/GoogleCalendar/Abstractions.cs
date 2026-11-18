using Kioskitos.Domain.Entities;

namespace Kioskitos.Application.GoogleCalendar;

public static class GoogleCalendarScopes
{
    // Only allows creating secondary calendars and managing the ones the app created (least privilege).
    public const string AppCreated = "https://www.googleapis.com/auth/calendar.app.created";
}

public sealed record GoogleTokenResult(string? RefreshToken, IReadOnlyCollection<string> GrantedScopes);

public interface IGoogleCalendarGateway
{
    /// <summary>Exchanges the authorization code for tokens. Returns null if the code is invalid.</summary>
    Task<GoogleTokenResult?> ExchangeCodeAsync(string code, CancellationToken ct);

    /// <summary>Creates a secondary calendar and returns its id.</summary>
    Task<string> CreateCalendarAsync(string refreshToken, string name, CancellationToken ct);
}

public interface ITokenProtector
{
    string Protect(string plainText);
    string Unprotect(string protectedText);
}

public interface IGoogleCalendarConnectionRepository
{
    Task<GoogleCalendarConnection?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task AddAsync(GoogleCalendarConnection connection, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}