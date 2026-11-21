using Kioskitos.Application.Tags;
using Kioskitos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kioskitos.Infrastructure.Persistence;

public sealed class TagRepository(AppDbContext db) : ITagRepository
{
    public async Task<IReadOnlyList<Tag>> ListByUserAsync(Guid userId, CancellationToken ct) =>
        await db.Tags
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Tag>> FindByNamesAsync(
        Guid userId, IReadOnlyCollection<string> names, CancellationToken ct)
    {
        var lowered = names.Select(n => n.ToLowerInvariant()).ToList();

        // Tracked on purpose: the returned tags are reused when creating a project,
        // and untracked ones would be inserted again on SaveChanges.
        return await db.Tags
            .Where(t => t.UserId == userId && lowered.Contains(t.Name.ToLower()))
            .ToListAsync(ct);
    }

    public async Task AddAsync(Tag tag, CancellationToken ct) =>
        await db.Tags.AddAsync(tag, ct);
}
