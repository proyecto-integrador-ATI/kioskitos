using Kioskitos.Domain.Entities;

namespace Kioskitos.Application.Clients;

internal static class ClientValidation
{
    // Client name and at least one complete contact person are required.
    public static bool IsValid(string name, IReadOnlyList<ClientContactData> contacts) =>
        !string.IsNullOrWhiteSpace(name)
        && contacts.Count > 0
        && contacts.All(c =>
            !string.IsNullOrWhiteSpace(c.Name)
            && !string.IsNullOrWhiteSpace(c.Email)
            && ClientContact.IsValidPhone(c.Phone));
}
