using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Kioskitos.Application.Users;
using Kioskitos.Domain.Entities;
using Kioskitos.Infrastructure.Auth;
using Kioskitos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kioskitos.API.Tests;

public sealed class UsersEndpointTests(KioskitosWebApplicationFactory factory)
    : IClassFixture<KioskitosWebApplicationFactory>
{
    private const string Route = "/api/users/me";

    [Fact]
    public async Task Get_WithoutJwt_ReturnsUnauthorized()
    {
        var response = await factory.CreateClient().GetAsync(Route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithoutJwt_ReturnsUnauthorized()
    {
        var response = await factory.CreateClient().PutAsJsonAsync(Route, new { name = "Name" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsProfileOfAuthenticatedUser()
    {
        var user = new User("google-sub", "user@example.com", "Test User");
        user.UpdateProfile("Test User", "Software", 25m);
        var client = CreateClient(new InMemoryUserRepository(user), user);

        var profile = await client.GetFromJsonAsync<UserProfileDto>(Route);

        Assert.NotNull(profile);
        Assert.Equal(user.Id, profile!.Id);
        Assert.Equal("user@example.com", profile.Email);
        Assert.Equal("Software", profile.Industry);
        Assert.Equal(25m, profile.HourlyRate);
    }

    [Fact]
    public async Task Get_WhenUserNoLongerExists_ReturnsNotFound()
    {
        var client = CreateClient(new InMemoryUserRepository(), new User("sub", "x@example.com", "X"));

        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_UpdatesOnlyEditableFieldsOfAuthenticatedUser()
    {
        var user = new User("google-sub", "user@example.com", "Test User");
        var other = new User("other-sub", "other@example.com", "Other User");
        var users = new InMemoryUserRepository(user, other);
        var client = CreateClient(users, user);

        // Identity fields in the body (even another user's id) must be ignored.
        var response = await client.PutAsJsonAsync(Route, new
        {
            id = other.Id,
            email = "hacked@example.com",
            googleSubject = "hacked-sub",
            name = "New Name",
            industry = "Software",
            hourlyRate = 42.5m
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.Equal(user.Id, profile!.Id);
        Assert.Equal("New Name", profile.Name);
        Assert.Equal("user@example.com", profile.Email);

        Assert.Equal("New Name", user.Name);
        Assert.Equal("Software", user.Industry);
        Assert.Equal(42.5m, user.HourlyRate);
        Assert.Equal("google-sub", user.GoogleSubject);
        Assert.Equal("user@example.com", user.Email);
        Assert.Equal("Other User", other.Name);
        Assert.Equal(1, users.SaveChangesCalls);
    }

    [Fact]
    public async Task Update_WithOptionalFieldsOmitted_ClearsThem()
    {
        var user = new User("google-sub", "user@example.com", "Test User");
        user.UpdateProfile("Test User", "Software", 10m);
        var client = CreateClient(new InMemoryUserRepository(user), user);

        var response = await client.PutAsJsonAsync(Route, new { name = "Test User" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(user.Industry);
        Assert.Null(user.HourlyRate);
    }

    public static TheoryData<object, string> InvalidRequests => new()
    {
        { new { name = "" }, "Name" },
        { new { name = "   " }, "Name" },
        { new { industry = "Software" }, "Name" },
        { new { name = new string('a', 201) }, "Name" },
        { new { name = "Name", industry = new string('a', 101) }, "Industry" },
        { new { name = "Name", hourlyRate = -1 }, "HourlyRate" },
        { new { name = "Name", hourlyRate = 10_000_000_000m }, "HourlyRate" }
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Update_WithInvalidData_ReturnsBadRequestAndDoesNotSave(object body, string invalidField)
    {
        var user = new User("google-sub", "user@example.com", "Test User");
        var users = new InMemoryUserRepository(user);
        var client = CreateClient(users, user);

        var response = await client.PutAsJsonAsync(Route, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(invalidField, problem!.Errors.Keys);
        Assert.Equal("Test User", user.Name);
        Assert.Equal(0, users.SaveChangesCalls);
    }

    [RequiresDatabaseFact]
    public async Task Update_PersistsChangesInDatabase()
    {
        var user = new User($"sub-{Guid.NewGuid()}", "db-user@example.com", "Db User");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        try
        {
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = Bearer(user);

            var response = await client.PutAsJsonAsync(Route, new
            {
                name = "Persisted Name",
                industry = "Design",
                hourlyRate = 15.75m
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var verifyScope = factory.Services.CreateScope();
            var saved = await verifyScope.ServiceProvider.GetRequiredService<AppDbContext>()
                .Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);

            Assert.Equal("Persisted Name", saved.Name);
            Assert.Equal("Design", saved.Industry);
            Assert.Equal(15.75m, saved.HourlyRate);
            Assert.Equal(user.GoogleSubject, saved.GoogleSubject);
            Assert.Equal(user.Email, saved.Email);
            Assert.Equal(user.CreatedAt, saved.CreatedAt, TimeSpan.FromMilliseconds(1));
        }
        finally
        {
            using var cleanupScope = factory.Services.CreateScope();
            await cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>()
                .Users.Where(u => u.Id == user.Id).ExecuteDeleteAsync();
        }
    }

    private HttpClient CreateClient(IUserRepository users, User authenticatedUser)
    {
        var client = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddSingleton(users)))
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
