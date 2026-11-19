using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Kioskitos.Application.GoogleCalendar;
using Kioskitos.Infrastructure.Auth;
using Microsoft.Extensions.Options;
using GoogleCalendarModel = Google.Apis.Calendar.v3.Data.Calendar;

namespace Kioskitos.Infrastructure.GoogleCalendar;

public sealed class GoogleCalendarGateway(IOptions<GoogleOptions> options) : IGoogleCalendarGateway
{
    // With Google Identity Services in popup mode, the redirect_uri for the exchange is "postmessage".
    private const string PopupRedirectUri = "postmessage";

    // The library requires a user identifier for its token store; nothing is stored here.
    private const string FlowUserId = "kioskitos";

    private const string TimeZone = "America/Montevideo";

    public async Task<GoogleTokenResult?> ExchangeCodeAsync(string code, CancellationToken ct)
    {
        try
        {
            using var flow = CreateFlow();
            var token = await flow.ExchangeCodeForTokenAsync(FlowUserId, code, PopupRedirectUri, ct);

            var scopes = (token.Scope ?? "")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return new GoogleTokenResult(token.RefreshToken, scopes);
        }
        catch (TokenResponseException)
        {
            return null;
        }
    }

    public async Task<string> CreateCalendarAsync(string refreshToken, string name, CancellationToken ct)
    {
        using var flow = CreateFlow();
        var credential = new UserCredential(flow, FlowUserId, new TokenResponse { RefreshToken = refreshToken });

        using var calendarService = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Kioskitos"
        });

        var created = await calendarService.Calendars
            .Insert(new GoogleCalendarModel { Summary = name, TimeZone = TimeZone })
            .ExecuteAsync(ct);

        return created.Id;
    }

    private GoogleAuthorizationCodeFlow CreateFlow() => new(new GoogleAuthorizationCodeFlow.Initializer
    {
        ClientSecrets = new ClientSecrets
        {
            ClientId = options.Value.ClientId,
            ClientSecret = options.Value.ClientSecret
        },
        Scopes = [GoogleCalendarScopes.AppCreated]
    });
}