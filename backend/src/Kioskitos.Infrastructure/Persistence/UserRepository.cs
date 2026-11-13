using Kioskitos.Application.Auth;
using Kioskitos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kioskitos.Infrastructure.Persistence;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByGoogleSubjectAsync(string googleSubject, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(u => u.GoogleSubject == googleSubject, ct);

    public async Task AddAsync(User user, CancellationToken ct) =>
        await db.Users.AddAsync(user, ct);

    public Task SaveChangesAsync(CancellationToken ct) =>
        db.SaveChangesAsync(ct);
}