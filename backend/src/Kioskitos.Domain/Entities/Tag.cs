namespace Kioskitos.Domain.Entities;

/// <summary>User-defined label used to classify projects.</summary>
public sealed class Tag
{
    public const int NameMaxLength = 50;

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = null!;

    private Tag() { } // EF Core

    public Tag(Guid userId, string name)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("The tag must belong to a user.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("The tag name is required.", nameof(name));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        Name = name.Trim();
    }
}
