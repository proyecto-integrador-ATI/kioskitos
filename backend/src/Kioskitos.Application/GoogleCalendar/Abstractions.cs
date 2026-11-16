using Kioskitos.Domain.Entities;

namespace Kioskitos.Application.GoogleCalendar;

public static class GoogleCalendarScopes
{
    // Solo permite crear calendarios secundarios y manejar los que creó la app (mínimo privilegio).
    public const string AppCreated = "https://www.googleapis.com/auth/calendar.app.created";
}

public sealed record GoogleTokenResult(string? RefreshToken, IReadOnlyCollection<string> GrantedScopes);

public interface IGoogleCalendarGateway
{
    /// <summary>Cambia el authorization code por tokens. Devuelve null si el code es inválido.</summary>
    Task<GoogleTokenResult?> ExchangeCodeAsync(string code, CancellationToken ct);

    /// <summary>Crea un calendario secundario y devuelve su id.</summary>
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