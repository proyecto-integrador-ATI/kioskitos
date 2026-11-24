using System.ComponentModel.DataAnnotations;
using Kioskitos.Domain.Entities;

namespace Kioskitos.API.Contracts;

public sealed record CreateProjectRequest(
    // Nullable so that a missing clientId is rejected instead of becoming Guid.Empty.
    [Required] Guid? ClientId,
    [Required, MaxLength(Project.NameMaxLength)] string Name,
    [MaxLength(Project.TextMaxLength)] string? Description,
    [MaxLength(Project.TextMaxLength)] string? Objectives,
    [MaxLength(Project.TextMaxLength)] string? Scope,
    bool HasStages,
    DateOnly? DueDate,
    ProjectStatus? Status,
    // Each tag's and stage's length is validated by CreateProjectService.
    List<string>? Tags,
    // Stage names in order. Required (at least one) when HasStages is true.
    List<string>? Stages);
