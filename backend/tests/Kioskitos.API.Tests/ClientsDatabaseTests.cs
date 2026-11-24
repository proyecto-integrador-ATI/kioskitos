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
    public async Task Update_PersistsDataAndReplacesContacts()
    {
        var user = new User($"sub-{Guid.NewGuid()}", "clients-update@example.com", "Db User");
        var client = new Client(user.Id, "Estudio Sur", "Diseño", null);
        client.AddContact("Ana", "ana@estudiosur.com", "099123456");
        client.AddContact("Luis", "luis@estudiosur.com", "098765432");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(user);
            db.Clients.Add(client);
            await db.SaveChangesAsync();
        }

        try
        {
            using (var scope = factory.Services.CreateScope())
            {
                var service = new UpdateClientService(
                    new ClientRepository(scope.ServiceProvider.GetRequiredService<AppDbContext>()));
                var command = new UpdateClientCommand("Estudio Norte", null, "Paga a 30 días.",
                [
                    new ClientContactData("Marta", "marta@estudionorte.com", "097111222"),
                    new ClientContactData("Ana", "ana@estudionorte.com", "099123456")
                ]);

                var result = await service.UpdateAsync(user.Id, client.Id, command, CancellationToken.None);
                Assert.True(result.Succeeded);
            }

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var saved = await new ClientQueryService(new ClientRepository(db))
                    .GetByIdAsync(user.Id, client.Id, CancellationToken.None);

                Assert.Equal("Estudio Norte", saved.Client!.Name);
                Assert.Null(saved.Client.Industry);
                Assert.Equal("Paga a 30 días.", saved.Client.Notes);
                Assert.Equal(["Marta", "Ana"], saved.Client.Contacts.Select(c => c.Name));
                // The previous contacts are deleted, not left orphaned.
                Assert.Equal(2, await db.ClientContacts.CountAsync(cc => cc.ClientId == client.Id));
            }
        }
        finally
        {
            using var cleanupScope = factory.Services.CreateScope();
            await cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>()
                .Users.Where(u => u.Id == user.Id).ExecuteDeleteAsync();
        }
    }

    [RequiresDatabaseFact]
    public async Task Delete_RemovesClientWithContactsAndKeepsClientsWithProjects()
    {
        var user = new User($"sub-{Guid.NewGuid()}", "clients-delete@example.com", "Db User");
        var client = new Client(user.Id, "Estudio Sur", null, null);
        client.AddContact("Ana", "ana@estudiosur.com", "099123456");
        client.AddContact("Luis", "luis@estudiosur.com", "098765432");
        var clientWithProject = new Client(user.Id, "Acme", null, null);
        clientWithProject.AddContact("Bob", "bob@acme.com", "099654321");
        var project = new Project(user.Id, clientWithProject.Id, "Website", null, null, null, false, null);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(user);
            db.Clients.AddRange(client, clientWithProject);
            db.Projects.Add(project);
            await db.SaveChangesAsync();
        }

        try
        {
            using (var scope = factory.Services.CreateScope())
            {
                var service = new DeleteClientService(
                    new ClientRepository(scope.ServiceProvider.GetRequiredService<AppDbContext>()));

                Assert.True((await service.DeleteAsync(user.Id, client.Id, CancellationToken.None)).Succeeded);
                Assert.Equal(
                    DeleteClientError.HasProjects,
                    (await service.DeleteAsync(user.Id, clientWithProject.Id, CancellationToken.None)).Error);
            }

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                Assert.False(await db.Clients.AnyAsync(c => c.Id == client.Id));
                Assert.False(await db.ClientContacts.AnyAsync(cc => cc.ClientId == client.Id));
                Assert.True(await db.Clients.AnyAsync(c => c.Id == clientWithProject.Id));
                Assert.True(await db.Projects.AnyAsync(p => p.Id == project.Id));
            }
        }
        finally
        {
            using var cleanupScope = factory.Services.CreateScope();
            await cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>()
                .Users.Where(u => u.Id == user.Id).ExecuteDeleteAsync();
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
