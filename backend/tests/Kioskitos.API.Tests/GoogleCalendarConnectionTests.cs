using Kioskitos.Application.GoogleCalendar;
using Kioskitos.Domain.Entities;

namespace Kioskitos.API.Tests;

public sealed class GoogleCalendarConnectionTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task Connect_CreatesCalendarAndStoresEncryptedToken()
    {
        var google = new FakeGoogleCalendarGateway(
            new GoogleTokenResult("refresh-token", [GoogleCalendarScopes.AppCreated]));
        var connections = new FakeConnectionRepository();
        var service = new ConnectGoogleCalendarService(google, new FakeTokenProtector(), connections);

        var result = await service.ConnectAsync(UserId, "valid-code", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("calendar-id", result.CalendarId);
        Assert.Equal(1, google.CreatedCalendars);
        Assert.Equal(ConnectGoogleCalendarService.CalendarName, google.LastCalendarName);

        var saved = Assert.Single(connections.Added);
        Assert.Equal(UserId, saved.UserId);
        Assert.Equal("protected:refresh-token", saved.EncryptedRefreshToken);
        Assert.NotEqual("refresh-token", saved.EncryptedRefreshToken);
    }

    [Fact]
    public async Task Connect_WithoutCalendarScope_ReturnsErrorAndCreatesNothing()
    {
        var google = new FakeGoogleCalendarGateway(
            new GoogleTokenResult("refresh-token", ["openid", "email"]));
        var connections = new FakeConnectionRepository();
        var service = new ConnectGoogleCalendarService(google, new FakeTokenProtector(), connections);

        var result = await service.ConnectAsync(UserId, "valid-code", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(ConnectGoogleCalendarError.MissingCalendarScope, result.Error);
        Assert.Equal(0, google.CreatedCalendars);
        Assert.Empty(connections.Added);
    }

    [Fact]
    public async Task Connect_WithInvalidCode_ReturnsError()
    {
        var google = new FakeGoogleCalendarGateway(null);
        var connections = new FakeConnectionRepository();
        var service = new ConnectGoogleCalendarService(google, new FakeTokenProtector(), connections);

        var result = await service.ConnectAsync(UserId, "invalid-code", CancellationToken.None);

        Assert.Equal(ConnectGoogleCalendarError.InvalidCode, result.Error);
        Assert.Equal(0, google.CreatedCalendars);
        Assert.Empty(connections.Added);
    }

    [Fact]
    public async Task Connect_WhenAlreadyConnected_DoesNotCreateSecondCalendar()
    {
        var existing = new GoogleCalendarConnection(
            UserId, "protected:old-token", "existing-calendar-id", GoogleCalendarScopes.AppCreated);
        var google = new FakeGoogleCalendarGateway(
            new GoogleTokenResult("new-token", [GoogleCalendarScopes.AppCreated]));
        var connections = new FakeConnectionRepository(existing);
        var service = new ConnectGoogleCalendarService(google, new FakeTokenProtector(), connections);

        var result = await service.ConnectAsync(UserId, "valid-code", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("existing-calendar-id", result.CalendarId);
        Assert.Equal(0, google.CreatedCalendars);
        Assert.Equal("protected:new-token", existing.EncryptedRefreshToken);
    }

    [Fact]
    public async Task Connect_WhenAlreadyConnectedAndNoNewRefreshToken_KeepsExistingToken()
    {
        var existing = new GoogleCalendarConnection(
            UserId, "protected:old-token", "existing-calendar-id", GoogleCalendarScopes.AppCreated);
        var google = new FakeGoogleCalendarGateway(
            new GoogleTokenResult(null, [GoogleCalendarScopes.AppCreated]));
        var connections = new FakeConnectionRepository(existing);
        var service = new ConnectGoogleCalendarService(google, new FakeTokenProtector(), connections);

        var result = await service.ConnectAsync(UserId, "valid-code", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("protected:old-token", existing.EncryptedRefreshToken);
    }

    [Fact]
    public async Task Connect_FirstTimeWithoutRefreshToken_ReturnsError()
    {
        var google = new FakeGoogleCalendarGateway(
            new GoogleTokenResult(null, [GoogleCalendarScopes.AppCreated]));
        var connections = new FakeConnectionRepository();
        var service = new ConnectGoogleCalendarService(google, new FakeTokenProtector(), connections);

        var result = await service.ConnectAsync(UserId, "valid-code", CancellationToken.None);

        Assert.Equal(ConnectGoogleCalendarError.MissingRefreshToken, result.Error);
        Assert.Equal(0, google.CreatedCalendars);
        Assert.Empty(connections.Added);
    }

    private sealed class FakeGoogleCalendarGateway(GoogleTokenResult? tokens) : IGoogleCalendarGateway
    {
        public int CreatedCalendars { get; private set; }
        public string? LastCalendarName { get; private set; }

        public Task<GoogleTokenResult?> ExchangeCodeAsync(string code, CancellationToken ct) =>
            Task.FromResult(tokens);

        public Task<string> CreateCalendarAsync(string refreshToken, string name, CancellationToken ct)
        {
            CreatedCalendars++;
            LastCalendarName = name;
            return Task.FromResult("calendar-id");
        }
    }

    private sealed class FakeTokenProtector : ITokenProtector
    {
        public string Protect(string plainText) => $"protected:{plainText}";
        public string Unprotect(string protectedText) => protectedText["protected:".Length..];
    }

    private sealed class FakeConnectionRepository(GoogleCalendarConnection? existing = null)
        : IGoogleCalendarConnectionRepository
    {
        public List<GoogleCalendarConnection> Added { get; } = [];

        public Task<GoogleCalendarConnection?> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
            Task.FromResult(existing);

        public Task AddAsync(GoogleCalendarConnection connection, CancellationToken ct)
        {
            Added.Add(connection);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }
}