using Kioskitos.Domain.Entities;

namespace Kioskitos.Application.Clients;

public interface IClientRepository
{
    Task AddAsync(Client client, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}