using Kioskitos.Domain.Entities;

namespace Kioskitos.Application.Projects;

public interface IProjectRepository
{
    Task AddAsync(Project project, CancellationToken ct);

    /// <summary>Returns the project with its tags, only if it belongs to the user.</summary>
    Task<Project?> GetByIdAsync(Guid userId, Guid projectId, CancellationToken ct);

    /// <summary>Returns the user's projects for a client, with their tags.</summary>
    Task<IReadOnlyList<Project>> ListByClientAsync(Guid userId, Guid clientId, CancellationToken ct);

    /// <summary>Returns every project of the user across all clients, with their tags and client name.</summary>
    Task<IReadOnlyList<ProjectWithClientName>> ListByUserAsync(Guid userId, CancellationToken ct);

    /// <summary>Saves every pending change of the unit of work (projects, stages and new tags).</summary>
    Task SaveChangesAsync(CancellationToken ct);
}

/// <summary>
/// Read model for the cross-client list. Project has no Client navigation, so the
/// repository joins the client and hands its name alongside the entity.
/// </summary>
public sealed record ProjectWithClientName(Project Project, string ClientName);

public interface IClientOwnershipChecker
{
    /// <summary>True if the client exists and belongs to the user.</summary>
    Task<bool> IsOwnedByAsync(Guid clientId, Guid userId, CancellationToken ct);
}
