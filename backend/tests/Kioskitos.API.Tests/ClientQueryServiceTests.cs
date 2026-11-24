using Kioskitos.Application.Clients;
using Kioskitos.Domain.Entities;

namespace Kioskitos.API.Tests;

public sealed class ClientQueryServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();

    private readonly InMemoryClientRepository _clients = new();
    private readonly ClientQueryService _service;

    public ClientQueryServiceTests()
    {
        _service = new ClientQueryService(_clients);
    }

    private async Task<Client> SeedAsync(
        string name, Guid? userId = null, string? industry = null, string? notes = null)
    {
        var client = new Client(userId ?? UserId, name, industry, notes);
        client.AddContact($"Contact of {name}", $"{name.ToLowerInvariant()}@mail.com", "099123456");
        await _clients.AddAsync(client, CancellationToken.None);
        await _clients.SaveChangesAsync(CancellationToken.None);
        return client;
    }

    [Fact]
    public async Task List_ReturnsOnlyTheUsersClients_SortedByName()
    {
        var zeta = await SeedAsync("Zeta");
        await SeedAsync("Foreign", userId: OtherUserId);
        var arbol = await SeedAsync("Árbol");
        var beta = await SeedAsync("beta");

        var list = await _service.ListAsync(UserId, CancellationToken.None);

        Assert.Equal([arbol.Id, beta.Id, zeta.Id], list.Select(c => c.Id));
    }

    [Fact]
    public async Task List_WithoutClients_ReturnsEmptyList()
    {
        var list = await _service.ListAsync(UserId, CancellationToken.None);

        Assert.Empty(list);
    }

    [Fact]
    public async Task List_ExposesTheFirstContactAsPrimary()
    {
        var client = new Client(UserId, "Estudio Sur", "Diseño", null);
        client.AddContact("Ana", "ana@estudiosur.com", "099123456");
        client.AddContact("Luis", "luis@estudiosur.com", "098765432");
        await _clients.AddAsync(client, CancellationToken.None);
        await _clients.SaveChangesAsync(CancellationToken.None);

        var summary = Assert.Single(await _service.ListAsync(UserId, CancellationToken.None));

        Assert.Equal("Estudio Sur", summary.Name);
        Assert.Equal("Diseño", summary.Industry);
        Assert.NotNull(summary.PrimaryContact);
        Assert.Equal("Ana", summary.PrimaryContact.Name);
        Assert.Equal("ana@estudiosur.com", summary.PrimaryContact.Email);
        Assert.Equal("099123456", summary.PrimaryContact.Phone);
    }

    [Fact]
    public async Task List_WithoutOptionalFields_ReturnsNulls()
    {
        await SeedAsync("Estudio Sur");

        var summary = Assert.Single(await _service.ListAsync(UserId, CancellationToken.None));

        Assert.Null(summary.Industry);
    }

    [Fact]
    public async Task GetById_ReturnsDetailWithAllContactsInOrder()
    {
        var client = new Client(UserId, "Estudio Sur", "Diseño", "Paga a 30 días.");
        client.AddContact("Ana", "ana@estudiosur.com", "099123456");
        client.AddContact("Luis", "luis@estudiosur.com", "098765432");
        await _clients.AddAsync(client, CancellationToken.None);
        await _clients.SaveChangesAsync(CancellationToken.None);

        var result = await _service.GetByIdAsync(UserId, client.Id, CancellationToken.None);

        Assert.True(result.Succeeded);
        var dto = result.Client!;
        Assert.Equal(client.Id, dto.Id);
        Assert.Equal("Estudio Sur", dto.Name);
        Assert.Equal("Diseño", dto.Industry);
        Assert.Equal("Paga a 30 días.", dto.Notes);
        Assert.Equal(["Ana", "Luis"], dto.Contacts.Select(c => c.Name));
    }

    [Fact]
    public async Task GetById_WithoutOptionalFields_ReturnsNulls()
    {
        var client = await SeedAsync("Estudio Sur");

        var result = await _service.GetByIdAsync(UserId, client.Id, CancellationToken.None);

        Assert.Null(result.Client!.Industry);
        Assert.Null(result.Client.Notes);
    }

    [Fact]
    public async Task GetById_OfAnotherUser_ReturnsNotFound()
    {
        var client = await SeedAsync("Foreign", userId: OtherUserId);

        var result = await _service.GetByIdAsync(UserId, client.Id, CancellationToken.None);

        Assert.Equal(ClientQueryError.NotFound, result.Error);
        Assert.Null(result.Client);
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNotFound()
    {
        var result = await _service.GetByIdAsync(UserId, Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(ClientQueryError.NotFound, result.Error);
    }
}
