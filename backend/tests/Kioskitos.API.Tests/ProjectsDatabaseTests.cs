using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kioskitos.Domain.Entities;
using Kioskitos.Infrastructure.Auth;
using Kioskitos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kioskitos.API.Tests;

/// <summary>End-to-end tests against the real database (EF configuration, repositories and FKs).</summary>
public sealed class ProjectsDatabaseTests(KioskitosWebApplicationFactory factory)
    : IClassFixture<KioskitosWebApplicationFactory>
{
    [RequiresDatabaseFact]
    public async Task CreateAndRead_PersistsProjectWithGenericStageAndReusedTags()
    {
        var user = new User($"sub-{Guid.NewGuid()}", "projects-db@example.com", "Db User");
        var otherUser = new User($"sub-{Guid.NewGuid()}", "projects-other@example.com", "Other");
        var client = new Client(user.Id, "Acme", null, null);
        client.AddContact("Ana", "ana@acme.com", "123");
        var foreignClient = new Client(otherUser.Id, "Foreign", null, null);
        foreignClient.AddContact("Bob", "bob@foreign.com", "456");
        var existingTag = new Tag(user.Id, "Branding");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.AddRange(user, otherUser);
            db.Clients.AddRange(client, foreignClient);
            db.Tags.Add(existingTag);
            await db.SaveChangesAsync();
        }

        try
        {
            var http = factory.CreateClient();
            http.DefaultRequestHeaders.Authorization = Bearer(user);

            var response = await http.PostAsJsonAsync("/api/projects", new
            {
                clientId = client.Id,
                name = "Website",
                dueDate = "2027-03-01",
                status = "InProgress",
                tags = new[] { "branding", "Web" }
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var projectId = (await response.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("id").GetGuid();

            // Detail and list read back from the database.
            var detail = await http.GetFromJsonAsync<JsonElement>($"/api/projects/{projectId}");
            Assert.Equal("InProgress", detail.GetProperty("status").GetString());
            Assert.Equal("2027-03-01", detail.GetProperty("dueDate").GetString());
            Assert.Equal(
                ["Branding", "Web"],
                detail.GetProperty("tags").EnumerateArray().Select(t => t.GetProperty("name").GetString()));

            var list = await http.GetFromJsonAsync<JsonElement>($"/api/clients/{client.Id}/projects");
            Assert.Equal(projectId, Assert.Single(list.EnumerateArray()).GetProperty("id").GetGuid());

            var tags = await http.GetFromJsonAsync<JsonElement>("/api/tags");
            Assert.Equal(2, tags.GetArrayLength());

            // The real ownership checker rejects another user's client.
            var foreign = await http.PostAsJsonAsync(
                "/api/projects", new { clientId = foreignClient.Id, name = "Nope" });
            Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
            Assert.Equal(
                HttpStatusCode.NotFound,
                (await http.GetAsync($"/api/clients/{foreignClient.Id}/projects")).StatusCode);

            using var verifyScope = factory.Services.CreateScope();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var stage = await verifyDb.Stages.AsNoTracking().SingleAsync(s => s.ProjectId == projectId);
            Assert.True(stage.IsGeneric);

            // "branding" reused the existing tag instead of creating a duplicate.
            var userTags = await verifyDb.Tags.AsNoTracking().Where(t => t.UserId == user.Id).ToListAsync();
            Assert.Equal(2, userTags.Count);
            Assert.Contains(userTags, t => t.Id == existingTag.Id);
        }
        finally
        {
            // Deleting the users cascades to clients, projects, stages and tags.
            using var cleanupScope = factory.Services.CreateScope();
            await cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>()
                .Users.Where(u => u.Id == user.Id || u.Id == otherUser.Id).ExecuteDeleteAsync();
        }
    }

    private AuthenticationHeaderValue Bearer(User user)
    {
        var jwtOptions = factory.Services.GetRequiredService<IOptions<JwtOptions>>();
        return new AuthenticationHeaderValue("Bearer", new JwtTokenGenerator(jwtOptions).Generate(user));
    }
}
