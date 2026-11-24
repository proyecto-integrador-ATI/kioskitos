using Kioskitos.Application.Clients;
using Kioskitos.Domain.Entities;

namespace Kioskitos.API.Tests;

public sealed class RegisterClientServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static readonly ClientContactData Ana =
        new("Ana Pérez", "ana@estudiosur.com", "099123456");

    [Fact]
    public async Task Register_SavesClientWithContactsForTheUser()
    {
        var repository = new InMemoryClientRepository();
        var service = new RegisterClientService(repository);
        var command = new RegisterClientCommand(
            "Estudio Sur", "Diseño", null,
            [Ana, new ClientContactData("Luis Gómez", "luis@estudiosur.com", "098765432")]);

        var result = await service.RegisterAsync(UserId, command, CancellationToken.None);

        Assert.True(result.Succeeded);
        var saved = Assert.Single(repository.Added);
        Assert.Equal(UserId, saved.UserId);
        Assert.Equal("Estudio Sur", saved.Name);
        Assert.Equal(2, saved.Contacts.Count);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task Register_WithoutContacts_ReturnsInvalidDataAndSavesNothing()
    {
        var repository = new InMemoryClientRepository();
        var service = new RegisterClientService(repository);
        var command = new RegisterClientCommand("Estudio Sur", null, null, []);

        var result = await service.RegisterAsync(UserId, command, CancellationToken.None);

        Assert.Equal(RegisterClientError.InvalidData, result.Error);
        Assert.Empty(repository.Added);
        Assert.False(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task Register_WithBlankName_ReturnsInvalidData()
    {
        var repository = new InMemoryClientRepository();
        var service = new RegisterClientService(repository);
        var command = new RegisterClientCommand("  ", null, null, [Ana]);

        var result = await service.RegisterAsync(UserId, command, CancellationToken.None);

        Assert.Equal(RegisterClientError.InvalidData, result.Error);
        Assert.Empty(repository.Added);
    }

    [Fact]
    public async Task Register_WithIncompleteContact_ReturnsInvalidData()
    {
        var repository = new InMemoryClientRepository();
        var service = new RegisterClientService(repository);
        var command = new RegisterClientCommand(
            "Estudio Sur", null, null, [new ClientContactData("Ana", "", "099123456")]);

        var result = await service.RegisterAsync(UserId, command, CancellationToken.None);

        Assert.Equal(RegisterClientError.InvalidData, result.Error);
        Assert.Empty(repository.Added);
    }

    [Fact]
    public async Task Register_WithInvalidPhone_ReturnsInvalidDataAndSavesNothing()
    {
        var repository = new InMemoryClientRepository();
        var service = new RegisterClientService(repository);
        var command = new RegisterClientCommand(
            "Estudio Sur", null, null, [new ClientContactData("Ana", "ana@estudiosur.com", "ss")]);

        var result = await service.RegisterAsync(UserId, command, CancellationToken.None);

        Assert.Equal(RegisterClientError.InvalidData, result.Error);
        Assert.Empty(repository.Added);
        Assert.False(repository.SaveChangesCalled);
    }
}
