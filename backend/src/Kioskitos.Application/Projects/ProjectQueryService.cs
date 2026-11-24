namespace Kioskitos.Application.Projects;

public sealed class ProjectQueryService(
    IProjectRepository projects,
    IClientOwnershipChecker clientOwnership)
{
    /// <summary>Returns the project detail, or null if it does not exist or belongs to another user.</summary>
    public async Task<ProjectDto?> GetByIdAsync(Guid userId, Guid projectId, CancellationToken ct)
    {
        var project = await projects.GetByIdAsync(userId, projectId, ct);
        return project?.ToDto();
    }

    /// <summary>
    /// Lists the client's projects, newest first. Returns null if the client does not exist
    /// or belongs to another user, so the caller can answer 404 instead of an empty list.
    /// </summary>
    public async Task<IReadOnlyList<ProjectSummaryDto>?> ListByClientAsync(
        Guid userId, Guid clientId, CancellationToken ct)
    {
        if (!await clientOwnership.IsOwnedByAsync(clientId, userId, ct))
        {
            return null;
        }

        var clientProjects = await projects.ListByClientAsync(userId, clientId, ct);

        return clientProjects
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => p.ToSummaryDto())
            .ToList();
    }

    /// <summary>Lists every project of the user across all clients, newest first.</summary>
    public async Task<IReadOnlyList<ProjectListItemDto>> ListAllAsync(Guid userId, CancellationToken ct)
    {
        var userProjects = await projects.ListByUserAsync(userId, ct);

        return userProjects
            .OrderByDescending(p => p.Project.CreatedAt)
            .Select(p => p.ToListItemDto())
            .ToList();
    }
}
