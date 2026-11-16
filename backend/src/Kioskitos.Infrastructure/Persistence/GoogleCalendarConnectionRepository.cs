using Kioskitos.Application.GoogleCalendar;
using Kioskitos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kioskitos.Infrastructure.Persistence;

public sealed class GoogleCalendarConnectionRepository(AppDbContext db)
    : IGoogleCalendarConnectionRepository
{
    public Task<GoogleCalendarConnection?> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        db.GoogleCalendarConnections.FirstOrDefaultAsync(c => c.UserId == userId, ct);

    public async Task AddAsync(GoogleCalendarConnection connection, CancellationToken ct) =>
        await db.GoogleCalendarConnections.AddAsync(connection, ct);

    public Task SaveChangesAsync(CancellationToken ct) =>
        db.SaveChangesAsync(ct);
}