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
            .Include(p => p.Stages)
            .ThenInclude(s => s.StageName)
            .AsSplitQuery() // avoids multiplying tag rows by stage rows
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId, ct);

    public async Task<IReadOnlyList<Project>> ListByClientAsync(
        Guid userId, Guid clientId, CancellationToken ct) =>
        await WithTags()
            .Where(p => p.ClientId == clientId && p.UserId == userId)
            .ToListAsync(ct);

    // Project has no Client navigation, so the name comes from an explicit join. Filtering the
    // client by user too keeps the query tenant-safe even if a project ever pointed elsewhere.
    public async Task<IReadOnlyList<ProjectWithClientName>> ListByUserAsync(
        Guid userId, CancellationToken ct) =>
        await (
            from project in WithTags()
            join client in db.Clients on project.ClientId equals client.Id
            where project.UserId == userId && client.UserId == userId
            select new ProjectWithClientName(project, client.Name))
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct) =>
        db.SaveChangesAsync(ct);

    // Read-only queries: no tracking needed.
    private IQueryable<Project> WithTags() => db.Projects
        .AsNoTracking()
        .Include(p => p.Tags)
        .ThenInclude(pt => pt.Tag);
}
