using Kioskitos.Domain.Entities;

namespace Kioskitos.Application.Clients;

public sealed record ClientContactData(string Name, string Email, string Phone);

public sealed record RegisterClientCommand(
    string Name,
    string? Industry,
    string? Notes,
    IReadOnlyList<ClientContactData> Contacts);

public enum RegisterClientError
{
    InvalidData
}

public sealed record RegisterClientResult(Client? Client, RegisterClientError? Error)
{
    public bool Succeeded => Error is null;

    public static RegisterClientResult Success(Client client) => new(client, null);
    public static RegisterClientResult Failure(RegisterClientError error) => new(null, error);
}

public sealed class RegisterClientService(IClientRepository clients)
{
    public async Task<RegisterClientResult> RegisterAsync(
        Guid userId, RegisterClientCommand command, CancellationToken ct)
    {
        // Client name and at least one complete contact person are required.
        if (string.IsNullOrWhiteSpace(command.Name)
            || command.Contacts.Count == 0
            || command.Contacts.Any(c =>
                string.IsNullOrWhiteSpace(c.Name)
                || string.IsNullOrWhiteSpace(c.Email)
                || !ClientContact.IsValidPhone(c.Phone)))
        {
            return RegisterClientResult.Failure(RegisterClientError.InvalidData);
        }

        // The owner always comes from the authenticated user, never from the request.
        var client = new Client(userId, command.Name, command.Industry, command.Notes);

        foreach (var contact in command.Contacts)
        {
            client.AddContact(contact.Name, contact.Email, contact.Phone);
        }

        await clients.AddAsync(client, ct);
        await clients.SaveChangesAsync(ct);

        return RegisterClientResult.Success(client);
    }
}