using Kioskitos.Application.Clients;
using Kioskitos.Domain.Entities;

namespace Kioskitos.Infrastructure.Persistence;

public sealed class ClientRepository(AppDbContext db) : IClientRepository
{
    public async Task AddAsync(Client client, CancellationToken ct) =>
        await db.Clients.AddAsync(client, ct);

    public Task SaveChangesAsync(CancellationToken ct) =>
        db.SaveChangesAsync(ct);
}