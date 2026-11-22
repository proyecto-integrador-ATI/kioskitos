using Kioskitos.Domain.Entities;

namespace Kioskitos.Application.Clients;

public sealed record ClientContactDto(Guid Id, string Name, string Email, string Phone);

public sealed record ClientDto(
    Guid Id,
    string Name,
    string? Industry,
    string? Notes,
    DateTime CreatedAt,
    IReadOnlyList<ClientContactDto> Contacts);

/// <summary>Row of the clients list. Only the primary contact is exposed, the detail has all of them.</summary>
public sealed record ClientSummaryDto(
    Guid Id,
    string Name,
    string? Industry,
    ClientContactDto? PrimaryContact);

internal static class ClientMappings
{
    public static ClientDto ToDto(this Client client) => new(
        client.Id,
        client.Name,
        client.Industry,
        client.Notes,
        client.CreatedAt,
        client.OrderedContacts().Select(ToDto).ToList());

    public static ClientSummaryDto ToSummaryDto(this Client client)
    {
        var primary = client.OrderedContacts().FirstOrDefault();
        return new(client.Id, client.Name, client.Industry, primary is null ? null : ToDto(primary));
    }

    private static IEnumerable<ClientContact> OrderedContacts(this Client client) =>
        client.Contacts.OrderBy(c => c.Position);

    private static ClientContactDto ToDto(ClientContact contact) =>
        new(contact.Id, contact.Name, contact.Email, contact.Phone);
}
