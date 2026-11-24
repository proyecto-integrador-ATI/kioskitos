using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kioskitos.Application.Projects;
using Kioskitos.Application.StageNames;
using Kioskitos.Application.Tags;
using Kioskitos.Domain.Entities;
using Kioskitos.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kioskitos.API.Tests;

public sealed class ProjectsEndpointTests(KioskitosWebApplicationFactory factory)
    : IClassFixture<KioskitosWebApplicationFactory>
{
    private readonly User _user = new("google-sub", "user@example.com", "Test User");
    private readonly User _otherUser = new("other-sub", "other@example.com", "Other User");
    private readonly Guid _clientId = Guid.NewGuid();
    private readonly Guid _otherClientId = Guid.NewGuid();

    private readonly InMemoryProjectRepository _projects = new();
    private InMemoryTagRepository _tags = new();
    private InMemoryStageNameRepository _stageNames = new();

    public static TheoryData<string, string> ProtectedRoutes => new()
    {
        { "POST", "/api/projects" },
        { "GET", "/api/projects" },
        { "GET", $"/api/projects/{Guid.NewGuid()}" },
        { "GET", $"/api/clients/{Guid.NewGuid()}/projects" },
        { "GET", "/api/tags" },
        { "GET", "/api/stage-names" }
    };

    [Theory]
    [MemberData(nameof(ProtectedRoutes))]
    public async Task Endpoints_WithoutJwt_ReturnUnauthorized(string method, string route)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), route)
        {
            Content = method == "POST" ? JsonContent.Create(new { name = "Website" }) : null
        };

        var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsCreatedWithLocationTagsAndStatusAsText()
    {
        _tags = new InMemoryTagRepository(new Tag(_user.Id, "Branding"));
        var client = CreateClient(_user);

        var response = await client.PostAsJsonAsync("/api/projects", new
        {
            clientId = _clientId,
            name = "Website",
            description = "Corporate site",
            hasStages = false,
            dueDate = "2027-03-01",
            status = "InProgress",
            tags = new[] { "branding", "Web" }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var saved = Assert.Single(_projects.Saved);
        Assert.Equal(_user.Id, saved.UserId);
        Assert.EndsWith($"/api/projects/{saved.Id}", response.Headers.Location!.ToString());

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("InProgress", json.GetProperty("status").GetString());
        Assert.Equal("2027-03-01", json.GetProperty("dueDate").GetString());
        Assert.Equal(
            ["Branding", "Web"],
            json.GetProperty("tags").EnumerateArray().Select(t => t.GetProperty("name").GetString()));
        Assert.Single(_tags.Added);
    }

    [Fact]
    public async Task Create_WithoutStatus_DefaultsToPlanning()
    {
        var response = await CreateClient(_user).PostAsJsonAsync(
            "/api/projects", new { clientId = _clientId, name = "Website" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Planning", json.GetProperty("status").GetString());
        Assert.Empty(json.GetProperty("tags").EnumerateArray());
    }

    public static TheoryData<object, string> InvalidRequests => new()
    {
        { new { clientId = Guid.NewGuid() }, "Name" },
        { new { clientId = Guid.NewGuid(), name = "   " }, "Name" },
        { new { clientId = Guid.NewGuid(), name = new string('a', Project.NameMaxLength + 1) }, "Name" },
        { new { name = "Website" }, "ClientId" },
        { new { clientId = Guid.NewGuid(), name = "Website", scope = new string('a', Project.TextMaxLength + 1) }, "Scope" },
        { new { clientId = Guid.NewGuid(), name = "Website", status = "Unknown" }, "$.status" }
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Create_WithInvalidData_ReturnsBadRequestAndSavesNothing(object body, string invalidField)
    {
        var response = await CreateClient(_user).PostAsJsonAsync("/api/projects", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(invalidField, problem!.Errors.Keys);
        Assert.Empty(_projects.Saved);
    }

    [Fact]
    public async Task Create_WithTooLongTag_ReturnsBadRequest()
    {
        var response = await CreateClient(_user).PostAsJsonAsync("/api/projects", new
        {
            clientId = _clientId,
            name = "Website",
            tags = new[] { new string('a', Tag.NameMaxLength + 1) }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("invalid_data", problem!.Extensions["errorCode"]?.ToString());
        Assert.Empty(_projects.Saved);
    }

    [Fact]
    public async Task Create_WithClientOfAnotherUser_ReturnsNotFound()
    {
        var response = await CreateClient(_user).PostAsJsonAsync(
            "/api/projects", new { clientId = _otherClientId, name = "Website" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("client_not_found", problem!.Extensions["errorCode"]?.ToString());
        Assert.Empty(_projects.Saved);
    }

    [Fact]
    public async Task GetById_ReturnsDetailOfOwnProject()
    {
        var project = await SeedAsync(_user.Id, _clientId, "Website");

        var response = await CreateClient(_user).GetAsync($"/api/projects/{project.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(project.Id, json.GetProperty("id").GetGuid());
        Assert.Equal(_clientId, json.GetProperty("clientId").GetGuid());
        Assert.Equal("Planning", json.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetById_OfAnotherUser_ReturnsNotFound()
    {
        var project = await SeedAsync(_otherUser.Id, _otherClientId, "Not mine");

        var response = await CreateClient(_user).GetAsync($"/api/projects/{project.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListByClient_ReturnsClientProjects()
    {
        var project = await SeedAsync(_user.Id, _clientId, "Website");

        var response = await CreateClient(_user).GetAsync($"/api/clients/{_clientId}/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var item = Assert.Single(json.EnumerateArray());
        Assert.Equal(project.Id, item.GetProperty("id").GetGuid());
        Assert.Equal("Planning", item.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ListByClient_OfAnotherUser_ReturnsNotFound()
    {
        await SeedAsync(_otherUser.Id, _otherClientId, "Not mine");

        var response = await CreateClient(_user).GetAsync($"/api/clients/{_otherClientId}/projects");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListAll_ReturnsOnlyOwnProjectsWithClientName()
    {
        _projects.ClientNames[_clientId] = "Acme";
        _projects.ClientNames[_otherClientId] = "Foreign";
        var project = await SeedAsync(_user.Id, _clientId, "Website");
        await SeedAsync(_otherUser.Id, _otherClientId, "Not mine");

        var response = await CreateClient(_user).GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var item = Assert.Single(json.EnumerateArray());
        Assert.Equal(project.Id, item.GetProperty("id").GetGuid());
        Assert.Equal(_clientId, item.GetProperty("clientId").GetGuid());
        Assert.Equal("Acme", item.GetProperty("clientName").GetString());
        Assert.Equal("Planning", item.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ListAll_WithoutProjects_ReturnsEmptyArray()
    {
        var response = await CreateClient(_user).GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Empty(json.EnumerateArray());
    }

    [Fact]
    public async Task ListTags_ReturnsOnlyTagsOfAuthenticatedUser()
    {
        _tags = new InMemoryTagRepository(
            new Tag(_user.Id, "Web"),
            new Tag(_otherUser.Id, "Foreign"),
            new Tag(_user.Id, "Branding"));

        var tags = await CreateClient(_user).GetFromJsonAsync<List<TagDto>>("/api/tags");

        Assert.Equal(["Branding", "Web"], tags!.Select(t => t.Name));
    }

    [Fact]
    public async Task Create_WithStages_ReturnsThemInOrder()
    {
        _stageNames = new InMemoryStageNameRepository(new StageName(_user.Id, "Design"));

        var response = await CreateClient(_user).PostAsJsonAsync("/api/projects", new
        {
            clientId = _clientId,
            name = "Website",
            hasStages = true,
            stages = new[] { "design", "Development" }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var stages = json.GetProperty("stages").EnumerateArray().ToList();
        Assert.Equal(["Design", "Development"], stages.Select(s => s.GetProperty("name").GetString()));
        Assert.All(stages, s => Assert.Equal("Planning", s.GetProperty("status").GetString()));
        Assert.Single(_stageNames.Added);
    }

    [Fact]
    public async Task Create_WithStagesButNoneSent_ReturnsBadRequest()
    {
        var response = await CreateClient(_user).PostAsJsonAsync(
            "/api/projects", new { clientId = _clientId, name = "Website", hasStages = true });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("invalid_data", problem!.Extensions["errorCode"]?.ToString());
        Assert.Empty(_projects.Saved);
    }

    [Fact]
    public async Task ListStageNames_ReturnsOnlyNamesOfAuthenticatedUser()
    {
        _stageNames = new InMemoryStageNameRepository(
            new StageName(_user.Id, "Development"),
            new StageName(_otherUser.Id, "Foreign"),
            new StageName(_user.Id, "Design"));

        var names = await CreateClient(_user).GetFromJsonAsync<List<StageNameDto>>("/api/stage-names");

        Assert.Equal(["Design", "Development"], names!.Select(n => n.Name));
    }

    private async Task<Project> SeedAsync(Guid userId, Guid clientId, string name)
    {
        var project = new Project(userId, clientId, name, null, null, null, false, null);
        await _projects.AddAsync(project, CancellationToken.None);
        await _projects.SaveChangesAsync(CancellationToken.None);
        return project;
    }

    private HttpClient CreateClient(User authenticatedUser)
    {
        var ownership = new FakeClientOwnershipChecker(
            (_clientId, _user.Id), (_otherClientId, _otherUser.Id));

        var client = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IProjectRepository>(_projects);
                services.AddSingleton<ITagRepository>(_tags);
                services.AddSingleton<IStageNameRepository>(_stageNames);
                services.AddSingleton<IClientOwnershipChecker>(ownership);
            }))
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
