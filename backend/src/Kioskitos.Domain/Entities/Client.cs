namespace Kioskitos.Domain.Entities;

public sealed class Client
{
    public const int NameMaxLength = 200;
    public const int IndustryMaxLength = 100;
    public const int NotesMaxLength = 2000;

    private readonly List<ClientContact> _contacts = [];

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    public string Name { get; private set; } = null!;
    public string? Industry { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<ClientContact> Contacts => _contacts.AsReadOnly();

    private Client() { } // EF Core

    public Client(Guid userId, string name, string? industry, string? notes)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("The client must belong to a user.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("The client name is required.", nameof(name));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        Name = name.Trim();
        Industry = Normalize(industry);
        Notes = Normalize(notes);
        CreatedAt = DateTime.UtcNow;
    }

    public ClientContact AddContact(string name, string email, string phone)
    {
        var contact = new ClientContact(Id, name, email, phone);
        _contacts.Add(contact);
        return contact;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}