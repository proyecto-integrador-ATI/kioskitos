using Kioskitos.Domain.Entities;

namespace Kioskitos.API.Contracts;

public sealed record ClientContactResponse(Guid Id, string Name, string Email, string Phone);

public sealed record ClientResponse(
    Guid Id,
    string Name,
    string? Industry,
    string? Notes,
    DateTime CreatedAt,
    IReadOnlyList<ClientContactResponse> Contacts)
{
    public static ClientResponse From(Client client) => new(
        client.Id,
        client.Name,
        client.Industry,
        client.Notes,
        client.CreatedAt,
        client.Contacts
            .Select(c => new ClientContactResponse(c.Id, c.Name, c.Email, c.Phone))
            .ToList());
}