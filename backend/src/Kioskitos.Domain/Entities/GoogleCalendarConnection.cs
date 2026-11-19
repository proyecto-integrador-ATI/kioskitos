namespace Kioskitos.Domain.Entities;

public sealed class GoogleCalendarConnection
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string EncryptedRefreshToken { get; private set; } = null!;
    public string CalendarId { get; private set; } = null!;
    public string GrantedScopes { get; private set; } = null!;
    public DateTime ConnectedAt { get; private set; }

    private GoogleCalendarConnection() { } // EF Core

    public GoogleCalendarConnection(
        Guid userId, string encryptedRefreshToken, string calendarId, string grantedScopes)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        EncryptedRefreshToken = encryptedRefreshToken;
        CalendarId = calendarId;
        GrantedScopes = grantedScopes;
        ConnectedAt = DateTime.UtcNow;
    }

    public void UpdateCredentials(string encryptedRefreshToken, string grantedScopes)
    {
        EncryptedRefreshToken = encryptedRefreshToken;
        GrantedScopes = grantedScopes;
        ConnectedAt = DateTime.UtcNow;
    }
}