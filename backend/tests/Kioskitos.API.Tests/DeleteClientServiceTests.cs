using Kioskitos.Application.Clients;
using Kioskitos.Domain.Entities;

namespace Kioskitos.API.Tests;

public sealed class DeleteClientServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task Delete_RemovesClientAndSaves()
    {
        var (repository, client) = Seed(UserId);
        var service = new DeleteClientService(repository);

        var result = await service.DeleteAsync(UserId, client.Id, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(client, Assert.Single(repository.Removed));
        Assert.True(repository.SaveChangesCalled);
        Assert.Empty(repository.Saved);
    }

    [Fact]
    public async Task Delete_ClientOfAnotherUser_ReturnsNotFoundAndDeletesNothing()
    {
        var (repository, client) = Seed(Guid.NewGuid());
        var service = new DeleteClientService(repository);

        var result = await service.DeleteAsync(UserId, client.Id, CancellationToken.None);

        Assert.Equal(DeleteClientError.NotFound, result.Error);
        Assert.Empty(repository.Removed);
        Assert.False(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task Delete_UnknownClient_ReturnsNotFound()
    {
        var (repository, _) = Seed(UserId);
        var service = new DeleteClientService(repository);

        var result = await service.DeleteAsync(UserId, Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(DeleteClientError.NotFound, result.Error);
        Assert.False(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task Delete_ClientWithProjects_ReturnsHasProjectsAndDeletesNothing()
    {
        var (repository, client) = Seed(UserId);
        repository.ClientsWithProjects.Add(client.Id);
        var service = new DeleteClientService(repository);

        var result = await service.DeleteAsync(UserId, client.Id, CancellationToken.None);

        Assert.Equal(DeleteClientError.HasProjects, result.Error);
        Assert.Empty(repository.Removed);
        Assert.False(repository.SaveChangesCalled);
    }

    private static (InMemoryClientRepository, Client) Seed(Guid ownerId)
    {
        var repository = new InMemoryClientRepository();
        var client = new Client(ownerId, "Estudio Sur", null, null);
        client.AddContact("Ana", "ana@estudiosur.com", "099123456");
        repository.Saved.Add(client);
        return (repository, client);
    }
}
