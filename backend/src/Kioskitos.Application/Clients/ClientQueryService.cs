namespace Kioskitos.Application.Clients;

public enum ClientQueryError
{
    NotFound
}

public sealed record GetClientResult(ClientDto? Client, ClientQueryError? Error)
{
    public bool Succeeded => Error is null;

    public static GetClientResult Success(ClientDto client) => new(client, null);
    public static GetClientResult Failure(ClientQueryError error) => new(null, error);
}

public sealed class ClientQueryService(IClientRepository clients)
{
    /// <summary>Lists the user's clients sorted by name.</summary>
    public async Task<IReadOnlyList<ClientSummaryDto>> ListAsync(Guid userId, CancellationToken ct)
    {
        var userClients = await clients.ListByUserAsync(userId, ct);

        // Invariant culture so "Árbol" sorts next to "Arbol" instead of after "Z".
        return userClients
            .OrderBy(c => c.Name, StringComparer.InvariantCultureIgnoreCase)
            .Select(c => c.ToSummaryDto())
            .ToList();
    }

    /// <summary>
    /// Returns the client detail. A client of another user is reported as NotFound,
    /// so its existence is not revealed.
    /// </summary>
    public async Task<GetClientResult> GetByIdAsync(Guid userId, Guid clientId, CancellationToken ct)
    {
        var client = await clients.GetByIdAsync(userId, clientId, ct);

        return client is null
            ? GetClientResult.Failure(ClientQueryError.NotFound)
            : GetClientResult.Success(client.ToDto());
    }
}
