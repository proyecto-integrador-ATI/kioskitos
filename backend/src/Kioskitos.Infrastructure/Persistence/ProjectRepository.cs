using Kioskitos.Application.Projects;
using Kioskitos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kioskitos.Infrastructure.Persistence;

public sealed class ProjectRepository(AppDbContext db) : IProjectRepository
{
    public async Task AddAsync(Project project, CancellationToken ct) =>
        await db.Projects.AddAsync(project, ct);

    public Task<Project?> GetByIdAsync(Guid userId, Guid projectId, CancellationToken ct) =>
        WithTags()
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId, ct);

    public async Task<IReadOnlyList<Project>> ListByClientAsync(
        Guid userId, Guid clientId, CancellationToken ct) =>
        await WithTags()
            .Where(p => p.ClientId == clientId && p.UserId == userId)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct) =>
        db.SaveChangesAsync(ct);

    // Read-only queries: no tracking needed.
    private IQueryable<Project> WithTags() => db.Projects
        .AsNoTracking()
        .Include(p => p.Tags)
        .ThenInclude(pt => pt.Tag);
}
