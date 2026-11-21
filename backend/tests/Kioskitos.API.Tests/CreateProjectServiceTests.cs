using Kioskitos.Application.Projects;
using Kioskitos.Domain.Entities;

namespace Kioskitos.API.Tests;

public sealed class CreateProjectServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();
    private static readonly Guid ClientId = Guid.NewGuid();

    private readonly InMemoryProjectRepository _projects = new();
    private readonly FakeClientOwnershipChecker _ownership = new((ClientId, UserId));

    private CreateProjectService CreateService(InMemoryTagRepository? tags = null) =>
        new(_projects, tags ?? new InMemoryTagRepository(), _ownership);

    private static CreateProjectCommand Command(
        string name = "Website",
        bool hasStages = false,
        ProjectStatus? status = null,
        string? description = null,
        Guid? clientId = null,
        params string[] tags) =>
        new(clientId ?? ClientId, name, description, "Launch", "Landing only",
            hasStages, new DateOnly(2027, 3, 1), status, tags);

    [Fact]
    public async Task Create_SavesProjectOnceAndReturnsDto()
    {
        var result = await CreateService().CreateAsync(UserId, Command(), CancellationToken.None);

        Assert.True(result.Succeeded);
        var saved = Assert.Single(_projects.Saved);
        Assert.Equal(1, _projects.SaveChangesCalls);
        Assert.Equal(UserId, saved.UserId);

        var dto = result.Project!;
        Assert.Equal(saved.Id, dto.Id);
        Assert.Equal(ClientId, dto.ClientId);
        Assert.Equal("Website", dto.Name);
        Assert.Equal("Launch", dto.Objectives);
        Assert.Equal(ProjectStatus.Planning, dto.Status);
        Assert.Equal(new DateOnly(2027, 3, 1), dto.DueDate);
        Assert.Empty(dto.Tags);
    }

    [Fact]
    public async Task Create_WithoutStages_CreatesGenericStage()
    {
        await CreateService().CreateAsync(UserId, Command(hasStages: false), CancellationToken.None);

        var stage = Assert.Single(Assert.Single(_projects.Saved).Stages);
        Assert.True(stage.IsGeneric);
    }

    [Fact]
    public async Task Create_WithStages_CreatesNoStage()
    {
        await CreateService().CreateAsync(UserId, Command(hasStages: true), CancellationToken.None);

        Assert.Empty(Assert.Single(_projects.Saved).Stages);
    }

    [Fact]
    public async Task Create_UsesGivenStatus()
    {
        var result = await CreateService().CreateAsync(
            UserId, Command(status: ProjectStatus.InProgress), CancellationToken.None);

        Assert.Equal(ProjectStatus.InProgress, result.Project!.Status);
    }

    [Fact]
    public async Task Create_ReusesExistingTagIgnoringCase_AndCreatesMissingOnes()
    {
        var existing = new Tag(UserId, "Branding");
        var tags = new InMemoryTagRepository(existing);

        var result = await CreateService(tags).CreateAsync(
            UserId, Command(tags: ["branding", " Web ", "WEB"]), CancellationToken.None);

        var newTag = Assert.Single(tags.Added);
        Assert.Equal("Web", newTag.Name);
        Assert.Equal(UserId, newTag.UserId);

        Assert.Equal(
            [existing.Id, newTag.Id],
            result.Project!.Tags.Select(t => t.Id));
        Assert.Equal(1, _projects.SaveChangesCalls);
    }

    [Fact]
    public async Task Create_DoesNotReuseTagsOfAnotherUser()
    {
        var foreign = new Tag(OtherUserId, "Web");
        var tags = new InMemoryTagRepository(foreign);

        var result = await CreateService(tags).CreateAsync(
            UserId, Command(tags: ["Web"]), CancellationToken.None);

        var created = Assert.Single(tags.Added);
        Assert.NotEqual(foreign.Id, created.Id);
        Assert.Equal(created.Id, Assert.Single(result.Project!.Tags).Id);
    }

    [Fact]
    public async Task Create_WithClientOfAnotherUser_ReturnsClientNotFound()
    {
        var tags = new InMemoryTagRepository();
        var result = await CreateService(tags).CreateAsync(
            OtherUserId, Command(tags: ["Web"]), CancellationToken.None);

        Assert.Equal(CreateProjectError.ClientNotFound, result.Error);
        Assert.Empty(_projects.Saved);
        Assert.Empty(tags.Added);
        Assert.Equal(0, _projects.SaveChangesCalls);
    }

    [Fact]
    public async Task Create_WithUnknownClient_ReturnsClientNotFound()
    {
        var result = await CreateService().CreateAsync(
            UserId, Command(clientId: Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(CreateProjectError.ClientNotFound, result.Error);
    }

    public static TheoryData<CreateProjectCommand> InvalidCommands => new()
    {
        Command(name: ""),
        Command(name: "   "),
        Command(name: new string('a', Project.NameMaxLength + 1)),
        Command(description: new string('a', Project.TextMaxLength + 1)),
        Command(status: (ProjectStatus)99),
        Command(clientId: Guid.Empty),
        Command(tags: [" "]),
        Command(tags: [new string('a', Tag.NameMaxLength + 1)]),
    };

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task Create_WithInvalidData_ReturnsInvalidDataAndSavesNothing(CreateProjectCommand command)
    {
        var result = await CreateService().CreateAsync(UserId, command, CancellationToken.None);

        Assert.Equal(CreateProjectError.InvalidData, result.Error);
        Assert.Empty(_projects.Saved);
        Assert.Equal(0, _projects.SaveChangesCalls);
    }
}
