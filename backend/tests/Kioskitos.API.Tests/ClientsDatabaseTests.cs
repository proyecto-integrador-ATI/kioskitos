using Kioskitos.Application.Clients;
using Kioskitos.Domain.Entities;
using Kioskitos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kioskitos.API.Tests;

/// <summary>Client queries against the real database (EF configuration, user filter, contact order).</summary>
public sealed class ClientsDatabaseTests(KioskitosWebApplicationFactory factory)
    : IClassFixture<KioskitosWebApplicationFactory>
{
    [RequiresDatabaseFact]
    public async Task ListAndGet_ReturnOnlyTheUsersClientsWithContactsInOrder()
    {
        var user = new User($"sub-{Guid.NewGuid()}", "clients-db@example.com", "Db User");
        var otherUser = new User($"sub-{Guid.NewGuid()}", "clients-other@example.com", "Other");

        var client = new Client(user.Id, "Estudio Sur", "Diseño", "Paga a 30 días.");
        // Several contacts so a random-id order would very likely differ from insertion order.
        var names = new[] { "Ana", "Luis", "Marta", "Pedro", "Sofía" };
        foreach (var name in names)
        {
            client.AddContact(name, $"{name.ToLowerInvariant()}@estudiosur.com", "099123456");
        }
        var bareClient = new Client(user.Id, "Acme", null, null);
        bareClient.AddContact("Bob", "bob@acme.com", "099654321");
        var foreignClient = new Client(otherUser.Id, "Foreign", null, null);
        foreignClient.AddContact("Eve", "eve@foreign.com", "099111222");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.AddRange(user, otherUser);
            db.Clients.AddRange(client, bareClient, foreignClient);
            await db.SaveChangesAsync();
        }

        try
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var repository = new ClientRepository(db);
            var service = new ClientQueryService(repository);

            // The list only has the user's clients, sorted by name, with the first contact as primary.
            var list = await service.ListAsync(user.Id, CancellationToken.None);
            Assert.Equal([bareClient.Id, client.Id], list.Select(c => c.Id));
            Assert.Null(list[0].Industry);
            Assert.Equal("Ana", list[1].PrimaryContact!.Name);

            // The detail keeps every field and the contacts in insertion order.
            var result = await service.GetByIdAsync(user.Id, client.Id, CancellationToken.None);
            Assert.True(result.Succeeded);
            Assert.Equal("Diseño", result.Client!.Industry);
            Assert.Equal("Paga a 30 días.", result.Client.Notes);
            Assert.Equal(names, result.Client.Contacts.Select(c => c.Name));

            // Another user's client is invisible even with its id.
            Assert.Null(await repository.GetByIdAsync(user.Id, foreignClient.Id, CancellationToken.None));
            Assert.Equal(
                ClientQueryError.NotFound,
                (await service.GetByIdAsync(user.Id, foreignClient.Id, CancellationToken.None)).Error);

            // Read-only queries must not leave tracked entities behind.
            Assert.Empty(db.ChangeTracker.Entries());

            var positions = await db.Set<ClientContact>()
                .AsNoTracking()
                .Where(cc => cc.ClientId == client.Id)
                .OrderBy(cc => cc.Position)
                .Select(cc => cc.Position)
                .ToListAsync();
            Assert.Equal([0, 1, 2, 3, 4], positions);
        }
        finally
        {
            // Deleting the users cascades to clients and contacts.
            using var cleanupScope = factory.Services.CreateScope();
            await cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>()
                .Users.Where(u => u.Id == user.Id || u.Id == otherUser.Id).ExecuteDeleteAsync();
        }
    }

    [RequiresDatabaseFact]
    public async Task ListByUser_WithoutClients_ReturnsEmptyList()
    {
        using var scope = factory.Services.CreateScope();
        var repository = new ClientRepository(scope.ServiceProvider.GetRequiredService<AppDbContext>());

        var clients = await repository.ListByUserAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(clients);
    }
}
