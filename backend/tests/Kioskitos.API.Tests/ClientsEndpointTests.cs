using System.Net;
using System.Net.Http.Json;

namespace Kioskitos.API.Tests;

public sealed class ClientsEndpointTests(KioskitosWebApplicationFactory factory)
    : IClassFixture<KioskitosWebApplicationFactory>
{
    [Fact]
    public async Task Register_WithoutJwt_ReturnsUnauthorized()
    {
        var http = factory.CreateClient();

        var response = await http.PostAsJsonAsync("/api/clients", new
        {
            name = "Estudio Sur",
            contacts = new[] { new { name = "Ana", email = "ana@estudiosur.com", phone = "099123456" } }
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}