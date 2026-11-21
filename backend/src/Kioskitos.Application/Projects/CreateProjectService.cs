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
    IReadOnlyList<string> TagNames);

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

        foreach (var tag in await ResolveTagsAsync(userId, command.TagNames, ct))
        {
            project.AddTag(tag);
        }

        await projects.AddAsync(project, ct);

        // Single save: the project, its generic stage and any new tag are stored together or not at all.
        await projects.SaveChangesAsync(ct);

        return CreateProjectResult.Success(project.ToDto());
    }

    /// <summary>Reuses the user's existing tags (ignoring case) and creates the missing ones.</summary>
    private async Task<IReadOnlyList<Tag>> ResolveTagsAsync(
        Guid userId, IReadOnlyList<string> tagNames, CancellationToken ct)
    {
        var names = tagNames
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (names.Count == 0)
        {
            return [];
        }

        var existing = await tags.FindByNamesAsync(userId, names, ct);
        var resolved = new List<Tag>(names.Count);

        foreach (var name in names)
        {
            var tag = existing.FirstOrDefault(t =>
                string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

            if (tag is null)
            {
                tag = new Tag(userId, name);
                await tags.AddAsync(tag, ct);
            }

            resolved.Add(tag);
        }

        return resolved;
    }

    private static bool IsValid(CreateProjectCommand command) =>
        command.ClientId != Guid.Empty
        && !string.IsNullOrWhiteSpace(command.Name)
        && command.Name.Trim().Length <= Project.NameMaxLength
        && FitsText(command.Description)
        && FitsText(command.Objectives)
        && FitsText(command.Scope)
        && (command.Status is null || Enum.IsDefined(command.Status.Value))
        && command.TagNames.All(n =>
            !string.IsNullOrWhiteSpace(n) && n.Trim().Length <= Tag.NameMaxLength);

    private static bool FitsText(string? value) =>
        value is null || value.Trim().Length <= Project.TextMaxLength;
}
