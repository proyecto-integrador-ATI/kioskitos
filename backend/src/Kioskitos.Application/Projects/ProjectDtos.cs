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
    IReadOnlyList<TagDto> Tags);

public sealed record ProjectSummaryDto(
    Guid Id,
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
        project.ToTagDtos());

    public static ProjectSummaryDto ToSummaryDto(this Project project) => new(
        project.Id,
        project.Name,
        project.Status,
        project.CreatedAt,
        project.DueDate,
        project.ToTagDtos());

    private static IReadOnlyList<TagDto> ToTagDtos(this Project project) => project.Tags
        .Select(pt => new TagDto(pt.Tag.Id, pt.Tag.Name))
        .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();
}
