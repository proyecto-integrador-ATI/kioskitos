using System.Text.RegularExpressions;

namespace Kioskitos.Domain.Entities;

public sealed partial class ClientContact
{
    public const int NameMaxLength = 200;
    public const int EmailMaxLength = 320;
    public const int PhoneMaxLength = 30;
    public const int PhoneMinDigits = 8;
    public const int PhoneMaxDigits = 15;

    public Guid Id { get; private set; }
    public Guid ClientId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string Phone { get; private set; } = null!;

    /// <summary>Order in which the contact was added. Position 0 is the primary contact.</summary>
    public int Position { get; private set; }

    private ClientContact() { } // EF Core

    // Internal: contacts are only created through Client.AddContact.
    internal ClientContact(Guid clientId, string name, string email, string phone, int position)
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

        if (!IsValidPhone(phone))
        {
            throw new ArgumentException("The contact phone is not valid.", nameof(phone));
        }

        Id = Guid.NewGuid();
        ClientId = clientId;
        Name = name.Trim();
        Email = email.Trim();
        Phone = phone.Trim();
        Position = position;
    }

    public static bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return false;
        }

        var trimmed = phone.Trim();
        var digits = trimmed.Count(char.IsAsciiDigit);

        return PhoneCharacters().IsMatch(trimmed)
            && digits is >= PhoneMinDigits and <= PhoneMaxDigits;
    }

    [GeneratedRegex(@"^\+?[0-9 ()-]+$")]
    private static partial Regex PhoneCharacters();
}