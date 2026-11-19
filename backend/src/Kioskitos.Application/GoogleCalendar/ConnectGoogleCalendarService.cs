using Kioskitos.Domain.Entities;

namespace Kioskitos.Application.GoogleCalendar;

public enum ConnectGoogleCalendarError
{
    InvalidCode,
    MissingCalendarScope,
    MissingRefreshToken
}

public sealed record ConnectGoogleCalendarResult(string? CalendarId, ConnectGoogleCalendarError? Error)
{
    public bool Succeeded => Error is null;

    public static ConnectGoogleCalendarResult Success(string calendarId) => new(calendarId, null);
    public static ConnectGoogleCalendarResult Failure(ConnectGoogleCalendarError error) => new(null, error);
}

public sealed class ConnectGoogleCalendarService(
    IGoogleCalendarGateway google,
    ITokenProtector tokenProtector,
    IGoogleCalendarConnectionRepository connections)
{
    public const string CalendarName = "Kioskitos";

    public async Task<ConnectGoogleCalendarResult> ConnectAsync(
        Guid userId, string code, CancellationToken ct)
    {
        var tokens = await google.ExchangeCodeAsync(code, ct);
        if (tokens is null)
        {
            return ConnectGoogleCalendarResult.Failure(ConnectGoogleCalendarError.InvalidCode);
        }

        // Google lets users grant only some permissions: check that the Calendar one was granted.
        if (!tokens.GrantedScopes.Contains(GoogleCalendarScopes.AppCreated))
        {
            return ConnectGoogleCalendarResult.Failure(ConnectGoogleCalendarError.MissingCalendarScope);
        }

        var grantedScopes = string.Join(' ', tokens.GrantedScopes);
        var existing = await connections.GetByUserIdAsync(userId, ct);

        // Already connected: do not create a second "Kioskitos" calendar.
        if (existing is not null)
        {
            // Google only sends a refresh token the first time consent is given.
            if (!string.IsNullOrWhiteSpace(tokens.RefreshToken))
            {
                existing.UpdateCredentials(tokenProtector.Protect(tokens.RefreshToken), grantedScopes);
                await connections.SaveChangesAsync(ct);
            }

            return ConnectGoogleCalendarResult.Success(existing.CalendarId);
        }

        if (string.IsNullOrWhiteSpace(tokens.RefreshToken))
        {
            return ConnectGoogleCalendarResult.Failure(ConnectGoogleCalendarError.MissingRefreshToken);
        }

        // RF 03.01: create the secondary "Kioskitos" calendar.
        var calendarId = await google.CreateCalendarAsync(tokens.RefreshToken, CalendarName, ct);

        var connection = new GoogleCalendarConnection(
            userId, tokenProtector.Protect(tokens.RefreshToken), calendarId, grantedScopes);

        await connections.AddAsync(connection, ct);
        await connections.SaveChangesAsync(ct);

        return ConnectGoogleCalendarResult.Success(calendarId);
    }
}