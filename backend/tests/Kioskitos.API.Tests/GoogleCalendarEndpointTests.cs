using System.Net;
using System.Net.Http.Json;

namespace Kioskitos.API.Tests;

public sealed class GoogleCalendarEndpointTests(KioskitosWebApplicationFactory factory)
    : IClassFixture<KioskitosWebApplicationFactory>
{
    [Fact]
    public async Task Connect_WithoutJwt_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/google-calendar/connect", new { code = "any-code" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}