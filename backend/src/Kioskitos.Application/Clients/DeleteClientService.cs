namespace Kioskitos.Application.Clients;

public enum DeleteClientError
{
    NotFound,
    HasProjects
}

public sealed record DeleteClientResult(DeleteClientError? Error)
{
    public bool Succeeded => Error is null;

    public static DeleteClientResult Success() => new((DeleteClientError?)null);
    public static DeleteClientResult Failure(DeleteClientError error) => new(error);
}

public sealed class DeleteClientService(IClientRepository clients)
{
    /// <summary>
    /// Deletes the client and its contacts. A client of another user is reported as NotFound,
    /// so its existence is not revealed.
    /// </summary>
    public async Task<DeleteClientResult> DeleteAsync(Guid userId, Guid clientId, CancellationToken ct)
    {
        var client = await clients.FindForUpdateAsync(userId, clientId, ct);
        if (client is null)
        {
            return DeleteClientResult.Failure(DeleteClientError.NotFound);
        }

        // The database would cascade the delete to the projects; refusing keeps them from being lost silently.
        if (await clients.HasProjectsAsync(clientId, ct))
        {
            return DeleteClientResult.Failure(DeleteClientError.HasProjects);
        }

        clients.Remove(client);
        await clients.SaveChangesAsync(ct);

        return DeleteClientResult.Success();
    }
}
