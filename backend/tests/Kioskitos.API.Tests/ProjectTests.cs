using Kioskitos.Domain.Entities;

namespace Kioskitos.API.Tests;

public sealed class ProjectTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ClientId = Guid.NewGuid();

    private static Project CreateProject(
        string name = "Website",
        bool hasStages = false,
        string? description = null,
        string? objectives = null,
        string? scope = null,
        DateOnly? dueDate = null) =>
        new(UserId, ClientId, name, description, objectives, scope, hasStages, dueDate);

    [Fact]
    public void Constructor_AssociatesProjectWithUserAndClient()
    {
        var project = CreateProject(name: "  Website  ");

        Assert.NotEqual(Guid.Empty, project.Id);
        Assert.Equal(UserId, project.UserId);
        Assert.Equal(ClientId, project.ClientId);
        Assert.Equal("Website", project.Name);
    }

    [Fact]
    public void Constructor_DefaultsStatusToPlanning()
    {
        Assert.Equal(ProjectStatus.Planning, CreateProject().Status);
    }

    [Fact]
    public void Constructor_UsesGivenStatus()
    {
        var project = new Project(
            UserId, ClientId, "Website", null, null, null, false, null, ProjectStatus.InProgress);

        Assert.Equal(ProjectStatus.InProgress, project.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithoutName_Throws(string? name)
    {
        Assert.Throws<ArgumentException>(() => CreateProject(name: name!));
    }

    [Fact]
    public void Constructor_WithoutClient_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new Project(UserId, Guid.Empty, "Website", null, null, null, false, null));
    }

    [Fact]
    public void Constructor_WithoutUser_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new Project(Guid.Empty, ClientId, "Website", null, null, null, false, null));
    }

    [Fact]
    public void Constructor_WithUndefinedStatus_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Project(UserId, ClientId, "Website", null, null, null, false, null, (ProjectStatus)99));
    }

    [Fact]
    public void Constructor_NormalizesEmptyOptionalFieldsToNull()
    {
        var project = CreateProject(description: "", objectives: "   ", scope: null);

        Assert.Null(project.Description);
        Assert.Null(project.Objectives);
        Assert.Null(project.Scope);
        Assert.Null(project.DueDate);
        Assert.Empty(project.Tags);
    }

    [Fact]
    public void Constructor_TrimsOptionalFields()
    {
        var dueDate = new DateOnly(2027, 3, 1);
        var project = CreateProject(
            description: " Description ", objectives: " Objectives ", scope: " Scope ", dueDate: dueDate);

        Assert.Equal("Description", project.Description);
        Assert.Equal("Objectives", project.Objectives);
        Assert.Equal("Scope", project.Scope);
        Assert.Equal(dueDate, project.DueDate);
    }

    [Fact]
    public void Constructor_WithoutStages_CreatesSingleGenericStage()
    {
        var project = CreateProject(hasStages: false);

        Assert.False(project.HasStages);
        var stage = Assert.Single(project.Stages);
        Assert.True(stage.IsGeneric);
        Assert.Equal(project.Id, stage.ProjectId);
        Assert.Equal(ProjectStatus.Planning, stage.Status);
        Assert.Null(stage.StageNameId);
        Assert.Null(stage.Description);
        Assert.Null(stage.Deliverable);
        Assert.Null(stage.EstimatedHours);
        Assert.Null(stage.DueDate);
    }

    [Fact]
    public void Constructor_WithStages_CreatesNoStages()
    {
        var project = CreateProject(hasStages: true);

        Assert.True(project.HasStages);
        Assert.Empty(project.Stages);
    }

    [Fact]
    public void AddTag_AssociatesTag()
    {
        var project = CreateProject();
        var tag = new Tag(UserId, "Web");

        project.AddTag(tag);

        var projectTag = Assert.Single(project.Tags);
        Assert.Equal(project.Id, projectTag.ProjectId);
        Assert.Equal(tag.Id, projectTag.TagId);
    }

    [Fact]
    public void AddTag_SameTagTwice_DoesNotDuplicate()
    {
        var project = CreateProject();
        var tag = new Tag(UserId, "Web");

        project.AddTag(tag);
        project.AddTag(tag);

        Assert.Single(project.Tags);
    }

    [Fact]
    public void AddTag_FromAnotherUser_Throws()
    {
        var project = CreateProject();
        var foreignTag = new Tag(Guid.NewGuid(), "Web");

        Assert.Throws<InvalidOperationException>(() => project.AddTag(foreignTag));
        Assert.Empty(project.Tags);
    }
}
