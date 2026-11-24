using Kioskitos.Application.Projects;
using Kioskitos.Domain.Entities;

namespace Kioskitos.API.Tests;

public sealed class ProjectQueryServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();
    private static readonly Guid ClientId = Guid.NewGuid();
    private static readonly Guid OtherClientId = Guid.NewGuid();

    private readonly InMemoryProjectRepository _projects = new();
    private readonly ProjectQueryService _service;

    public ProjectQueryServiceTests()
    {
        var ownership = new FakeClientOwnershipChecker(
            (ClientId, UserId), (OtherClientId, UserId));
        _service = new ProjectQueryService(_projects, ownership);
    }

    private async Task<Project> SeedAsync(string name, Guid? clientId = null, Guid? userId = null)
    {
        var project = new Project(
            userId ?? UserId, clientId ?? ClientId, name, null, null, null, false, null);
        await _projects.AddAsync(project, CancellationToken.None);
        await _projects.SaveChangesAsync(CancellationToken.None);
        return project;
    }

    [Fact]
    public async Task GetById_ReturnsDetailWithTagsSortedByName()
    {
        var project = await SeedAsync("Website");
        project.AddTag(new Tag(UserId, "web"));
        project.AddTag(new Tag(UserId, "Branding"));

        var dto = await _service.GetByIdAsync(UserId, project.Id, CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal("Website", dto.Name);
        Assert.Equal(["Branding", "web"], dto.Tags.Select(t => t.Name));
    }

    [Fact]
    public async Task GetById_OfAnotherUser_ReturnsNull()
    {
        var project = await SeedAsync("Website");

        var dto = await _service.GetByIdAsync(OtherUserId, project.Id, CancellationToken.None);

        Assert.Null(dto);
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNull()
    {
        Assert.Null(await _service.GetByIdAsync(UserId, Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task ListByClient_ReturnsOnlyThatClientsProjects_NewestFirst()
    {
        var first = await SeedAsync("First");
        await SeedAsync("Other client", clientId: OtherClientId);
        await Task.Delay(5); // CreatedAt must differ for the ordering to be observable.
        var second = await SeedAsync("Second");

        var list = await _service.ListByClientAsync(UserId, ClientId, CancellationToken.None);

        Assert.NotNull(list);
        Assert.Equal([second.Id, first.Id], list.Select(p => p.Id));
    }

    [Fact]
    public async Task ListByClient_WithoutProjects_ReturnsEmptyList()
    {
        var list = await _service.ListByClientAsync(UserId, ClientId, CancellationToken.None);

        Assert.NotNull(list);
        Assert.Empty(list);
    }

    [Fact]
    public async Task ListByClient_OfAnotherUser_ReturnsNull()
    {
        await SeedAsync("Website");

        var list = await _service.ListByClientAsync(OtherUserId, ClientId, CancellationToken.None);

        Assert.Null(list);
    }

    [Fact]
    public async Task ListAll_ReturnsOnlyUserProjectsAcrossClients_NewestFirst()
    {
        _projects.ClientNames[ClientId] = "Acme";
        _projects.ClientNames[OtherClientId] = "Globex";
        var first = await SeedAsync("First");
        await SeedAsync("Another user's", userId: OtherUserId);
        await Task.Delay(5); // CreatedAt must differ for the ordering to be observable.
        var second = await SeedAsync("Second", clientId: OtherClientId);

        var list = await _service.ListAllAsync(UserId, CancellationToken.None);

        Assert.Equal([second.Id, first.Id], list.Select(p => p.Id));
    }

    [Fact]
    public async Task ListAll_IncludesClientAndTagsSortedByName()
    {
        _projects.ClientNames[ClientId] = "Acme";
        var project = await SeedAsync("Website");
        project.AddTag(new Tag(UserId, "web"));
        project.AddTag(new Tag(UserId, "Branding"));

        var item = Assert.Single(await _service.ListAllAsync(UserId, CancellationToken.None));

        Assert.Equal(ClientId, item.ClientId);
        Assert.Equal("Acme", item.ClientName);
        Assert.Equal("Website", item.Name);
        Assert.Equal(["Branding", "web"], item.Tags.Select(t => t.Name));
    }

    [Fact]
    public async Task ListAll_WithoutProjects_ReturnsEmptyList()
    {
        Assert.Empty(await _service.ListAllAsync(UserId, CancellationToken.None));
    }
}
