using Kioskitos.Application.Projects;
using Microsoft.EntityFrameworkCore;

namespace Kioskitos.Infrastructure.Persistence;

public sealed class ClientOwnershipChecker(AppDbContext db) : IClientOwnershipChecker
{
    public Task<bool> IsOwnedByAsync(Guid clientId, Guid userId, CancellationToken ct) =>
        db.Clients.AnyAsync(c => c.Id == clientId && c.UserId == userId, ct);
}
