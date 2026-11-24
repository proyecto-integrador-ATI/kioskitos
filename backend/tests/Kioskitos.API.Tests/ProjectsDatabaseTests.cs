using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kioskitos.Application.Projects;
using Kioskitos.Application.Tags;
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
        client.AddContact("Ana", "ana@acme.com", "099123456");
        var foreignClient = new Client(otherUser.Id, "Foreign", null, null);
        foreignClient.AddContact("Bob", "bob@foreign.com", "099654321");
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
            Assert.Empty(detail.GetProperty("stages").EnumerateArray()); // the generic stage is hidden

            var tags = await http.GetFromJsonAsync<JsonElement>("/api/tags");
            Assert.Equal(2, tags.GetArrayLength());

            // A project with stages stores them in order and reuses the user's stage names.
            var withStages = await http.PostAsJsonAsync("/api/projects", new
            {
                clientId = client.Id,
                name = "App",
                hasStages = true,
                stages = new[] { "Design", "Development" }
            });
            Assert.Equal(HttpStatusCode.Created, withStages.StatusCode);
            var withStagesId = (await withStages.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("id").GetGuid();
            var withStagesDetail = await http.GetFromJsonAsync<JsonElement>($"/api/projects/{withStagesId}");
            Assert.Equal(
                ["Design", "Development"],
                withStagesDetail.GetProperty("stages").EnumerateArray().Select(s => s.GetProperty("name").GetString()));

            var again = await http.PostAsJsonAsync("/api/projects", new
            {
                clientId = client.Id,
                name = "App 2",
                hasStages = true,
                stages = new[] { "design" }
            });
            Assert.Equal(HttpStatusCode.Created, again.StatusCode);
            var stageNames = await http.GetFromJsonAsync<JsonElement>("/api/stage-names");
            Assert.Equal(2, stageNames.GetArrayLength());

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

    [RequiresDatabaseFact]
    public async Task ListByUser_ReturnsOwnProjectsAcrossClientsWithClientNameAndTags()
    {
        var user = new User($"sub-{Guid.NewGuid()}", "projects-list@example.com", "List User");
        var otherUser = new User($"sub-{Guid.NewGuid()}", "projects-list-other@example.com", "Other");
        var acme = new Client(user.Id, "Acme", null, null);
        acme.AddContact("Ana", "ana@acme.com", "099123456");
        var globex = new Client(user.Id, "Globex", null, null);
        globex.AddContact("Gus", "gus@globex.com", "099111222");
        var foreignClient = new Client(otherUser.Id, "Foreign", null, null);
        foreignClient.AddContact("Bob", "bob@foreign.com", "099654321");

        var website = new Project(user.Id, acme.Id, "Website", null, null, null, false, null);
        website.AddTag(new Tag(user.Id, "web"));
        website.AddTag(new Tag(user.Id, "Branding"));
        var app = new Project(user.Id, globex.Id, "App", null, null, null, false, null);
        var foreignProject = new Project(otherUser.Id, foreignClient.Id, "Nope", null, null, null, false, null);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.AddRange(user, otherUser);
            db.Clients.AddRange(acme, globex, foreignClient);
            db.Projects.AddRange(website, app, foreignProject);
            await db.SaveChangesAsync();
        }

        try
        {
            using var scope = factory.Services.CreateScope();
            var repository = new ProjectRepository(scope.ServiceProvider.GetRequiredService<AppDbContext>());

            var items = await repository.ListByUserAsync(user.Id, CancellationToken.None);

            Assert.Equal(2, items.Count);
            var websiteItem = Assert.Single(items, i => i.Project.Id == website.Id);
            Assert.Equal("Acme", websiteItem.ClientName);
            Assert.Equal(
                ["Branding", "web"],
                websiteItem.Project.Tags.Select(pt => pt.Tag.Name).Order(StringComparer.OrdinalIgnoreCase));
            Assert.Equal("Globex", Assert.Single(items, i => i.Project.Id == app.Id).ClientName);
        }
        finally
        {
            // Deleting the users cascades to clients, projects, stages and tags.
            using var cleanupScope = factory.Services.CreateScope();
            await cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>()
                .Users.Where(u => u.Id == user.Id || u.Id == otherUser.Id).ExecuteDeleteAsync();
        }
    }

    [RequiresDatabaseFact]
    public async Task TrySaveChanges_ReturnsFalse_WhenTheSameTagNameAlreadyExistsIgnoringCase()
    {
        var user = new User($"sub-{Guid.NewGuid()}", "projects-conflict@example.com", "Conflict User");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(user);
            db.Tags.Add(new Tag(user.Id, "Web"));
            await db.SaveChangesAsync();
        }

        try
        {
            // Simulates a request that did not find "WEB" and tries to insert it
            // after another request already saved "Web".
            using var scope = factory.Services.CreateScope();
            var tags = scope.ServiceProvider.GetRequiredService<ITagRepository>();
            var projects = scope.ServiceProvider.GetRequiredService<IProjectRepository>();

            await tags.AddAsync(new Tag(user.Id, "WEB"), CancellationToken.None);

            Assert.False(await projects.TrySaveChangesAsync(CancellationToken.None));

            // The lookup by name ignores case too.
            var found = await tags.FindByNamesAsync(user.Id, ["wEb"], CancellationToken.None);
            Assert.Equal("Web", Assert.Single(found).Name);
        }
        finally
        {
            using var cleanupScope = factory.Services.CreateScope();
            await cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>()
                .Users.Where(u => u.Id == user.Id).ExecuteDeleteAsync();
        }
    }

    private AuthenticationHeaderValue Bearer(User user)
    {
        var jwtOptions = factory.Services.GetRequiredService<IOptions<JwtOptions>>();
        return new AuthenticationHeaderValue("Bearer", new JwtTokenGenerator(jwtOptions).Generate(user));
    }
}
