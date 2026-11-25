using Kioskitos.Application.Clients;
using Kioskitos.Domain.Entities;

namespace Kioskitos.API.Tests;

/// <summary>In-memory client store. Added clients are only visible after SaveChanges, like EF.</summary>
internal sealed class InMemoryClientRepository : IClientRepository
{
    public List<Client> Added { get; } = [];
    public List<Client> Saved { get; } = [];
    public bool SaveChangesCalled { get; private set; }

    public Task AddAsync(Client client, CancellationToken ct)
    {
        Added.Add(client);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Client>> ListByUserAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Client>>(Saved.Where(c => c.UserId == userId).ToList());

    public Task<Client?> GetByIdAsync(Guid userId, Guid clientId, CancellationToken ct) =>
        Task.FromResult(Saved.FirstOrDefault(c => c.Id == clientId && c.UserId == userId));

    public Task<Client?> FindForUpdateAsync(Guid userId, Guid clientId, CancellationToken ct) =>
        GetByIdAsync(userId, clientId, ct);

    /// <summary>Ids of the clients that have projects.</summary>
    public HashSet<Guid> ClientsWithProjects { get; } = [];

    public Task<bool> HasProjectsAsync(Guid clientId, CancellationToken ct) =>
        Task.FromResult(ClientsWithProjects.Contains(clientId));

    public List<Client> Removed { get; } = [];

    public void Remove(Client client) => Removed.Add(client);

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveChangesCalled = true;
        Saved.AddRange(Added.Except(Saved));
        Saved.RemoveAll(Removed.Contains);
        return Task.CompletedTask;
    }
}
