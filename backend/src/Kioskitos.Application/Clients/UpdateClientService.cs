namespace Kioskitos.Application.Clients;

public sealed record UpdateClientCommand(
    string Name,
    string? Industry,
    string? Notes,
    IReadOnlyList<ClientContactData> Contacts);

public enum UpdateClientError
{
    NotFound,
    InvalidData
}

public sealed record UpdateClientResult(ClientDto? Client, UpdateClientError? Error)
{
    public bool Succeeded => Error is null;

    public static UpdateClientResult Success(ClientDto client) => new(client, null);
    public static UpdateClientResult Failure(UpdateClientError error) => new(null, error);
}

public sealed class UpdateClientService(IClientRepository clients)
{
    /// <summary>
    /// Replaces the client data and its contacts. A client of another user is reported as NotFound,
    /// so its existence is not revealed.
    /// </summary>
    public async Task<UpdateClientResult> UpdateAsync(
        Guid userId, Guid clientId, UpdateClientCommand command, CancellationToken ct)
    {
        if (!ClientValidation.IsValid(command.Name, command.Contacts))
        {
            return UpdateClientResult.Failure(UpdateClientError.InvalidData);
        }

        var client = await clients.FindForUpdateAsync(userId, clientId, ct);
        if (client is null)
        {
            return UpdateClientResult.Failure(UpdateClientError.NotFound);
        }

        client.Update(command.Name, command.Industry, command.Notes);
        client.ReplaceContacts(command.Contacts.Select(c => (c.Name, c.Email, c.Phone)));

        await clients.SaveChangesAsync(ct);

        return UpdateClientResult.Success(client.ToDto());
    }
}
