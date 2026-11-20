namespace Kioskitos.Domain.Entities;

public sealed class ClientContact
{
    public const int NameMaxLength = 200;
    public const int EmailMaxLength = 320;
    public const int PhoneMaxLength = 30;

    public Guid Id { get; private set; }
    public Guid ClientId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string Phone { get; private set; } = null!;

    private ClientContact() { } // EF Core

    // Internal: contacts are only created through Client.AddContact.
    internal ClientContact(Guid clientId, string name, string email, string phone)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("The contact name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("The contact email is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new ArgumentException("The contact phone is required.", nameof(phone));
        }

        Id = Guid.NewGuid();
        ClientId = clientId;
        Name = name.Trim();
        Email = email.Trim();
        Phone = phone.Trim();
    }
}