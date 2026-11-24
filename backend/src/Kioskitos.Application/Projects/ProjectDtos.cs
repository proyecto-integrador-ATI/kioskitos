using Kioskitos.Application.Tags;
using Kioskitos.Domain.Entities;

namespace Kioskitos.Application.Projects;

public sealed record ProjectDto(
    Guid Id,
    Guid ClientId,
    string Name,
    string? Description,
    string? Objectives,
    string? Scope,
    ProjectStatus Status,
    bool HasStages,
    DateTime CreatedAt,
    DateOnly? DueDate,
    IReadOnlyList<TagDto> Tags,
    IReadOnlyList<StageDto> Stages);

/// <summary>Named stage of a project. The generic stage is never exposed.</summary>
public sealed record StageDto(Guid Id, string Name, ProjectStatus Status);

public sealed record ProjectSummaryDto(
    Guid Id,
    string Name,
    ProjectStatus Status,
    DateTime CreatedAt,
    DateOnly? DueDate,
    IReadOnlyList<TagDto> Tags);

/// <summary>Project row of the cross-client list; carries the client so the UI can tell projects apart.</summary>
public sealed record ProjectListItemDto(
    Guid Id,
    Guid ClientId,
    string ClientName,
    string Name,
    ProjectStatus Status,
    DateTime CreatedAt,
    DateOnly? DueDate,
    IReadOnlyList<TagDto> Tags);

internal static class ProjectMappings
{
    public static ProjectDto ToDto(this Project project) => new(
        project.Id,
        project.ClientId,
        project.Name,
        project.Description,
        project.Objectives,
        project.Scope,
        project.Status,
        project.HasStages,
        project.CreatedAt,
        project.DueDate,
        project.ToTagDtos(),
        project.Stages
            .Where(s => !s.IsGeneric)
            .OrderBy(s => s.Position)
            .Select(s => new StageDto(s.Id, s.StageName!.Name, s.Status))
            .ToList());

    public static ProjectSummaryDto ToSummaryDto(this Project project) => new(
        project.Id,
        project.Name,
        project.Status,
        project.CreatedAt,
        project.DueDate,
        project.ToTagDtos());

    public static ProjectListItemDto ToListItemDto(this ProjectWithClientName item) => new(
        item.Project.Id,
        item.Project.ClientId,
        item.ClientName,
        item.Project.Name,
        item.Project.Status,
        item.Project.CreatedAt,
        item.Project.DueDate,
        item.Project.ToTagDtos());

    private static IReadOnlyList<TagDto> ToTagDtos(this Project project) => project.Tags
        .Select(pt => new TagDto(pt.Tag.Id, pt.Tag.Name))
        .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();
}
