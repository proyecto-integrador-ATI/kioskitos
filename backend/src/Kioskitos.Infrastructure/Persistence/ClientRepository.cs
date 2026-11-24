using Kioskitos.Application.Clients;
using Kioskitos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kioskitos.Infrastructure.Persistence;

public sealed class ClientRepository(AppDbContext db) : IClientRepository
{
    public async Task AddAsync(Client client, CancellationToken ct) =>
        await db.Clients.AddAsync(client, ct);

    public async Task<IReadOnlyList<Client>> ListByUserAsync(Guid userId, CancellationToken ct) =>
        await db.Clients
            .AsNoTracking()
            .Include(c => c.Contacts)
            .Where(c => c.UserId == userId)
            .ToListAsync(ct);

    public Task<Client?> GetByIdAsync(Guid userId, Guid clientId, CancellationToken ct) =>
        db.Clients
            .AsNoTracking()
            .Include(c => c.Contacts)
            .FirstOrDefaultAsync(c => c.Id == clientId && c.UserId == userId, ct);

    public Task SaveChangesAsync(CancellationToken ct) =>
        db.SaveChangesAsync(ct);
}
