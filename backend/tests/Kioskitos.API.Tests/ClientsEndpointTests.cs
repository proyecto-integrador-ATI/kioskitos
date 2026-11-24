using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kioskitos.Application.Clients;
using Kioskitos.Domain.Entities;
using Kioskitos.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kioskitos.API.Tests;

public sealed class ClientsEndpointTests(KioskitosWebApplicationFactory factory)
    : IClassFixture<KioskitosWebApplicationFactory>
{
    private readonly User _user = new("google-sub", "user@example.com", "Test User");
    private readonly User _otherUser = new("other-sub", "other@example.com", "Other User");

    private readonly InMemoryClientRepository _clients = new();

    public static TheoryData<string, string> ProtectedRoutes => new()
    {
        { "POST", "/api/clients" },
        { "GET", "/api/clients" },
        { "GET", $"/api/clients/{Guid.NewGuid()}" }
    };

    [Theory]
    [MemberData(nameof(ProtectedRoutes))]
    public async Task Endpoints_WithoutJwt_ReturnUnauthorized(string method, string route)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), route)
        {
            Content = method == "POST"
                ? JsonContent.Create(new
                {
                    name = "Estudio Sur",
                    contacts = new[] { new { name = "Ana", email = "ana@estudiosur.com", phone = "099123456" } }
                })
                : null
        };

        var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsCreatedWithLocationAndClientDetail()
    {
        var response = await CreateClient(_user).PostAsJsonAsync("/api/clients", new
        {
            name = "Estudio Sur",
            industry = "Diseño",
            contacts = new[] { new { name = "Ana", email = "ana@estudiosur.com", phone = "099123456" } }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var saved = Assert.Single(_clients.Saved);
        Assert.Equal(_user.Id, saved.UserId);
        Assert.EndsWith($"/api/clients/{saved.Id}", response.Headers.Location!.ToString());

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Estudio Sur", json.GetProperty("name").GetString());
        Assert.Equal("Ana", Assert.Single(json.GetProperty("contacts").EnumerateArray())
            .GetProperty("name").GetString());
    }

    [Fact]
    public async Task List_ReturnsOnlyClientsOfAuthenticatedUserSortedByName()
    {
        await SeedAsync(_user.Id, "Zeta");
        await SeedAsync(_otherUser.Id, "Foreign");
        await SeedAsync(_user.Id, "Acme");

        var list = await CreateClient(_user).GetFromJsonAsync<JsonElement>("/api/clients");

        Assert.Equal(
            ["Acme", "Zeta"],
            list.EnumerateArray().Select(c => c.GetProperty("name").GetString()));
    }

    [Fact]
    public async Task List_SerializesPrimaryContactAndMissingOptionalFieldsAsNull()
    {
        await SeedAsync(_user.Id, "Acme");

        var list = await CreateClient(_user).GetFromJsonAsync<JsonElement>("/api/clients");

        var summary = Assert.Single(list.EnumerateArray());
        Assert.Equal(JsonValueKind.Null, summary.GetProperty("industry").ValueKind);
        var contact = summary.GetProperty("primaryContact");
        Assert.Equal("Ana", contact.GetProperty("name").GetString());
        Assert.Equal("ana@acme.com", contact.GetProperty("email").GetString());
        Assert.Equal("099123456", contact.GetProperty("phone").GetString());
        // Internal data stays out of the list.
        Assert.False(summary.TryGetProperty("userId", out _));
        Assert.False(summary.TryGetProperty("notes", out _));
    }

    [Fact]
    public async Task List_WithoutClients_ReturnsEmptyArray()
    {
        var list = await CreateClient(_user).GetFromJsonAsync<JsonElement>("/api/clients");

        Assert.Equal(0, list.GetArrayLength());
    }

    [Fact]
    public async Task GetById_ReturnsDetailWithNotesAndContacts()
    {
        var client = await SeedAsync(_user.Id, "Acme", industry: "Diseño", notes: "Paga a 30 días.");

        var json = await CreateClient(_user).GetFromJsonAsync<JsonElement>($"/api/clients/{client.Id}");

        Assert.Equal(client.Id, json.GetProperty("id").GetGuid());
        Assert.Equal("Diseño", json.GetProperty("industry").GetString());
        Assert.Equal("Paga a 30 días.", json.GetProperty("notes").GetString());
        Assert.Single(json.GetProperty("contacts").EnumerateArray());
        Assert.False(json.TryGetProperty("userId", out _));
    }

    [Fact]
    public async Task GetById_OfAnotherUser_ReturnsNotFound()
    {
        var foreign = await SeedAsync(_otherUser.Id, "Foreign");

        var response = await CreateClient(_user).GetAsync($"/api/clients/{foreign.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("client_not_found", problem!.Extensions["errorCode"]?.ToString());
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNotFound()
    {
        var response = await CreateClient(_user).GetAsync($"/api/clients/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Client> SeedAsync(
        Guid userId, string name, string? industry = null, string? notes = null)
    {
        var client = new Client(userId, name, industry, notes);
        client.AddContact("Ana", $"ana@{name.ToLowerInvariant()}.com", "099123456");
        await _clients.AddAsync(client, CancellationToken.None);
        await _clients.SaveChangesAsync(CancellationToken.None);
        return client;
    }

    private HttpClient CreateClient(User authenticatedUser)
    {
        var client = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddSingleton<IClientRepository>(_clients)))
            .CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(authenticatedUser);
        return client;
    }

    private AuthenticationHeaderValue Bearer(User user)
    {
        var jwtOptions = factory.Services.GetRequiredService<IOptions<JwtOptions>>();
        return new AuthenticationHeaderValue("Bearer", new JwtTokenGenerator(jwtOptions).Generate(user));
    }
}
