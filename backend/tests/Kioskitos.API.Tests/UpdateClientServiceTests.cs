using Kioskitos.Application.Clients;
using Kioskitos.Domain.Entities;

namespace Kioskitos.API.Tests;

public sealed class UpdateClientServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static readonly ClientContactData Luis =
        new("Luis Gómez", "luis@estudiosur.com", "098765432");

    [Fact]
    public async Task Update_ReplacesDataAndContactsAndSaves()
    {
        var (repository, client) = await SeedAsync(UserId);
        var service = new UpdateClientService(repository);
        var command = new UpdateClientCommand("Estudio Norte", "Fotografía", "Paga a 30 días.", [Luis]);

        var result = await service.UpdateAsync(UserId, client.Id, command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Estudio Norte", result.Client!.Name);
        Assert.Equal("Fotografía", result.Client.Industry);
        Assert.Equal("Luis Gómez", Assert.Single(result.Client.Contacts).Name);
        Assert.Equal("Estudio Norte", client.Name);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task Update_ClientOfAnotherUser_ReturnsNotFoundAndChangesNothing()
    {
        var (repository, client) = await SeedAsync(Guid.NewGuid());
        var service = new UpdateClientService(repository);
        var command = new UpdateClientCommand("Estudio Norte", null, null, [Luis]);

        var result = await service.UpdateAsync(UserId, client.Id, command, CancellationToken.None);

        Assert.Equal(UpdateClientError.NotFound, result.Error);
        Assert.Equal("Estudio Sur", client.Name);
        Assert.False(repository.SaveChangesCalled);
    }

    [Theory]
    [InlineData(" ", "Luis", "luis@estudiosur.com", "098765432")]
    [InlineData("Estudio Norte", "", "luis@estudiosur.com", "098765432")]
    [InlineData("Estudio Norte", "Luis", "luis@estudiosur.com", "ss")]
    public async Task Update_WithInvalidData_ReturnsInvalidDataAndChangesNothing(
        string name, string contactName, string email, string phone)
    {
        var (repository, client) = await SeedAsync(UserId);
        var service = new UpdateClientService(repository);
        var command = new UpdateClientCommand(name, null, null, [new(contactName, email, phone)]);

        var result = await service.UpdateAsync(UserId, client.Id, command, CancellationToken.None);

        Assert.Equal(UpdateClientError.InvalidData, result.Error);
        Assert.Equal("Ana", Assert.Single(client.Contacts).Name);
        Assert.False(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task Update_WithoutContacts_ReturnsInvalidData()
    {
        var (repository, client) = await SeedAsync(UserId);
        var service = new UpdateClientService(repository);

        var result = await service.UpdateAsync(
            UserId, client.Id, new UpdateClientCommand("Estudio Sur", null, null, []), CancellationToken.None);

        Assert.Equal(UpdateClientError.InvalidData, result.Error);
    }

    private static Task<(InMemoryClientRepository, Client)> SeedAsync(Guid ownerId)
    {
        var repository = new InMemoryClientRepository();
        var client = new Client(ownerId, "Estudio Sur", null, null);
        client.AddContact("Ana", "ana@estudiosur.com", "099123456");
        // Seeded directly so SaveChangesCalled only reflects the update.
        repository.Saved.Add(client);
        return Task.FromResult((repository, client));
    }
}
