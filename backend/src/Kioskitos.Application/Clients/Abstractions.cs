using Kioskitos.Domain.Entities;

namespace Kioskitos.Application.Clients;

public interface IClientRepository
{
    /// <summary>Adds a new client and its contacts to the unit of work.</summary>
    Task AddAsync(Client client, CancellationToken ct);

    /// <summary>Returns every client of the user, with their contacts.</summary>
    Task<IReadOnlyList<Client>> ListByUserAsync(Guid userId, CancellationToken ct);

    /// <summary>Returns the client with its contacts, only if it belongs to the user.</summary>
    Task<Client?> GetByIdAsync(Guid userId, Guid clientId, CancellationToken ct);

    /// <summary>Returns the tracked client with its contacts, only if it belongs to the user.</summary>
    Task<Client?> FindForUpdateAsync(Guid userId, Guid clientId, CancellationToken ct);

    /// <summary>Saves every pending change of the unit of work.</summary>
    Task SaveChangesAsync(CancellationToken ct);
}
