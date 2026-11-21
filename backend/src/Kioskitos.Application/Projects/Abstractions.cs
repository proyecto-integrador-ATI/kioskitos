using Kioskitos.Domain.Entities;

namespace Kioskitos.Application.Projects;

public interface IProjectRepository
{
    Task AddAsync(Project project, CancellationToken ct);

    /// <summary>Returns the project with its tags, only if it belongs to the user.</summary>
    Task<Project?> GetByIdAsync(Guid userId, Guid projectId, CancellationToken ct);

    /// <summary>Returns the user's projects for a client, with their tags.</summary>
    Task<IReadOnlyList<Project>> ListByClientAsync(Guid userId, Guid clientId, CancellationToken ct);

    /// <summary>Saves every pending change of the unit of work (projects, stages and new tags).</summary>
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IClientOwnershipChecker
{
    /// <summary>True if the client exists and belongs to the user.</summary>
    Task<bool> IsOwnedByAsync(Guid clientId, Guid userId, CancellationToken ct);
}
