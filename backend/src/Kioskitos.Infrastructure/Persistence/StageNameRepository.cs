using Kioskitos.Application.StageNames;
using Kioskitos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kioskitos.Infrastructure.Persistence;

public sealed class StageNameRepository(AppDbContext db) : IStageNameRepository
{
    public async Task<IReadOnlyList<StageName>> ListByUserAsync(Guid userId, CancellationToken ct) =>
        await db.StageNames
            .AsNoTracking()
            .Where(sn => sn.UserId == userId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StageName>> FindByNamesAsync(
        Guid userId, IReadOnlyCollection<string> names, CancellationToken ct)
    {
        // Name is citext, so this comparison already ignores case.
        // Tracked on purpose: reused names would be inserted again on SaveChanges otherwise.
        return await db.StageNames
            .Where(sn => sn.UserId == userId && names.Contains(sn.Name))
            .ToListAsync(ct);
    }

    public async Task AddAsync(StageName stageName, CancellationToken ct) =>
        await db.StageNames.AddAsync(stageName, ct);
}
