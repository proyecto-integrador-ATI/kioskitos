using Kioskitos.Application.Projects;
using Kioskitos.Application.StageNames;
using Kioskitos.Application.Tags;
using Kioskitos.Domain.Entities;

namespace Kioskitos.API.Tests;

/// <summary>In-memory project store. Added projects are only visible after SaveChanges, like EF.</summary>
internal sealed class InMemoryProjectRepository : IProjectRepository
{
    private readonly List<Project> _pending = [];

    public List<Project> Saved { get; } = [];
    public int SaveChangesCalls { get; private set; }

    public Task AddAsync(Project project, CancellationToken ct)
    {
        _pending.Add(project);
        return Task.CompletedTask;
    }

    public Task<Project?> GetByIdAsync(Guid userId, Guid projectId, CancellationToken ct) =>
        Task.FromResult(Saved.FirstOrDefault(p => p.Id == projectId && p.UserId == userId));

    public Task<IReadOnlyList<Project>> ListByClientAsync(Guid userId, Guid clientId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Project>>(
            Saved.Where(p => p.UserId == userId && p.ClientId == clientId).ToList());

    /// <summary>Client names by id. Projects whose client is not here are left out, like the SQL inner join.</summary>
    public Dictionary<Guid, string> ClientNames { get; } = [];

    public Task<IReadOnlyList<ProjectWithClientName>> ListByUserAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ProjectWithClientName>>(
            Saved
                .Where(p => p.UserId == userId && ClientNames.ContainsKey(p.ClientId))
                .Select(p => new ProjectWithClientName(p, ClientNames[p.ClientId]))
                .ToList());

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveChangesCalls++;
        Saved.AddRange(_pending);
        _pending.Clear();
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryTagRepository(params Tag[] existing) : ITagRepository
{
    public List<Tag> All { get; } = [.. existing];
    public List<Tag> Added { get; } = [];

    public Task<IReadOnlyList<Tag>> ListByUserAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Tag>>(All.Where(t => t.UserId == userId).ToList());

    public Task<IReadOnlyList<Tag>> FindByNamesAsync(
        Guid userId, IReadOnlyCollection<string> names, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Tag>>(All
            .Where(t => t.UserId == userId
                        && names.Contains(t.Name, StringComparer.OrdinalIgnoreCase))
            .ToList());

    public Task AddAsync(Tag tag, CancellationToken ct)
    {
        All.Add(tag);
        Added.Add(tag);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryStageNameRepository(params StageName[] existing) : IStageNameRepository
{
    public List<StageName> All { get; } = [.. existing];
    public List<StageName> Added { get; } = [];

    public Task<IReadOnlyList<StageName>> ListByUserAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<StageName>>(All.Where(sn => sn.UserId == userId).ToList());

    public Task<IReadOnlyList<StageName>> FindByNamesAsync(
        Guid userId, IReadOnlyCollection<string> names, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<StageName>>(All
            .Where(sn => sn.UserId == userId
                         && names.Contains(sn.Name, StringComparer.OrdinalIgnoreCase))
            .ToList());

    public Task AddAsync(StageName stageName, CancellationToken ct)
    {
        All.Add(stageName);
        Added.Add(stageName);
        return Task.CompletedTask;
    }
}

internal sealed class FakeClientOwnershipChecker(params (Guid ClientId, Guid UserId)[] owned)
    : IClientOwnershipChecker
{
    public Task<bool> IsOwnedByAsync(Guid clientId, Guid userId, CancellationToken ct) =>
        Task.FromResult(owned.Contains((clientId, userId)));
}
