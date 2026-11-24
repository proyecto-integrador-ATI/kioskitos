using Kioskitos.Application.StageNames;
using Kioskitos.Application.Tags;
using Kioskitos.Domain.Entities;

namespace Kioskitos.Application.Projects;

public sealed record CreateProjectCommand(
    Guid ClientId,
    string Name,
    string? Description,
    string? Objectives,
    string? Scope,
    bool HasStages,
    DateOnly? DueDate,
    ProjectStatus? Status,
    IReadOnlyList<string> TagNames,
    IReadOnlyList<string> StageNames);

public enum CreateProjectError
{
    InvalidData,
    ClientNotFound
}

public sealed record CreateProjectResult(ProjectDto? Project, CreateProjectError? Error)
{
    public bool Succeeded => Error is null;

    public static CreateProjectResult Success(ProjectDto project) => new(project, null);
    public static CreateProjectResult Failure(CreateProjectError error) => new(null, error);
}

public sealed class CreateProjectService(
    IProjectRepository projects,
    ITagRepository tags,
    IStageNameRepository stageNames,
    IClientOwnershipChecker clientOwnership)
{
    public async Task<CreateProjectResult> CreateAsync(
        Guid userId, CreateProjectCommand command, CancellationToken ct)
    {
        if (!IsValid(command))
        {
            return CreateProjectResult.Failure(CreateProjectError.InvalidData);
        }

        // A client of another user is reported as not found, so its existence is not revealed.
        if (!await clientOwnership.IsOwnedByAsync(command.ClientId, userId, ct))
        {
            return CreateProjectResult.Failure(CreateProjectError.ClientNotFound);
        }

        var project = await BuildProjectAsync(userId, command, ct);

        // Single save: the project, its stages and any new tag or stage name are stored together or not at all.
        // If another request of the same user saved one of our new tags or stage names in the meantime,
        // the save fails. Then we build the project again: this time that name exists and is reused.
        if (!await projects.TrySaveChangesAsync(ct))
        {
            project = await BuildProjectAsync(userId, command, ct);
            await projects.SaveChangesAsync(ct);
        }

        return CreateProjectResult.Success(project.ToDto());
    }

    /// <summary>Creates the project with its tags and stages and adds everything to the unit of work, without saving.</summary>
    private async Task<Project> BuildProjectAsync(Guid userId, CreateProjectCommand command, CancellationToken ct)
    {
        var project = new Project(
            userId,
            command.ClientId,
            command.Name,
            command.Description,
            command.Objectives,
            command.Scope,
            command.HasStages,
            command.DueDate,
            command.Status ?? ProjectStatus.Planning);

        var projectTags = await ResolveByNameAsync(
            Clean(command.TagNames),
            names => tags.FindByNamesAsync(userId, names, ct),
            name => new Tag(userId, name),
            tag => tags.AddAsync(tag, ct),
            tag => tag.Name);

        foreach (var tag in projectTags)
        {
            project.AddTag(tag);
        }

        // Stages keep the order in which they were sent.
        var projectStageNames = await ResolveByNameAsync(
            Clean(command.StageNames),
            names => stageNames.FindByNamesAsync(userId, names, ct),
            name => new StageName(userId, name),
            stageName => stageNames.AddAsync(stageName, ct),
            stageName => stageName.Name);

        foreach (var stageName in projectStageNames)
        {
            project.AddStage(stageName);
        }

        await projects.AddAsync(project, ct);

        return project;
    }

    /// <summary>Reuses the user's existing entities with those names (ignoring case) and creates the missing ones.</summary>
    private static async Task<IReadOnlyList<T>> ResolveByNameAsync<T>(
        IReadOnlyList<string> names,
        Func<IReadOnlyList<string>, Task<IReadOnlyList<T>>> findExisting,
        Func<string, T> create,
        Func<T, Task> add,
        Func<T, string> nameOf)
    {
        if (names.Count == 0)
        {
            return [];
        }

        var existing = await findExisting(names);
        var resolved = new List<T>(names.Count);

        foreach (var name in names)
        {
            var entity = existing.FirstOrDefault(e =>
                string.Equals(nameOf(e), name, StringComparison.OrdinalIgnoreCase));

            if (entity is null)
            {
                entity = create(name);
                await add(entity);
            }

            resolved.Add(entity);
        }

        return resolved;
    }

    /// <summary>Trims the names and drops repeated ones (ignoring case), keeping the first occurrence.</summary>
    private static IReadOnlyList<string> Clean(IReadOnlyList<string> names) => names
        .Select(n => n.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    private static bool IsValid(CreateProjectCommand command) =>
        command.ClientId != Guid.Empty
        && !string.IsNullOrWhiteSpace(command.Name)
        && command.Name.Trim().Length <= Project.NameMaxLength
        && FitsText(command.Description)
        && FitsText(command.Objectives)
        && FitsText(command.Scope)
        && (command.Status is null || Enum.IsDefined(command.Status.Value))
        && AreValidNames(command.TagNames, Tag.NameMaxLength)
        && AreValidStages(command);

    // With stages: at least one, without repeating names. Without stages: none (only the generic one).
    private static bool AreValidStages(CreateProjectCommand command) =>
        command.HasStages
            ? command.StageNames.Count > 0
              && AreValidNames(command.StageNames, StageName.NameMaxLength)
              && Clean(command.StageNames).Count == command.StageNames.Count
            : command.StageNames.Count == 0;

    private static bool AreValidNames(IReadOnlyList<string> names, int maxLength) =>
        names.All(n => !string.IsNullOrWhiteSpace(n) && n.Trim().Length <= maxLength);

    private static bool FitsText(string? value) =>
        value is null || value.Trim().Length <= Project.TextMaxLength;
}
