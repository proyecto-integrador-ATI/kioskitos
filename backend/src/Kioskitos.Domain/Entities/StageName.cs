namespace Kioskitos.Domain.Entities;

/// <summary>Reusable stage name defined by a user (e.g. "Design", "Development").</summary>
public sealed class StageName
{
    public const int NameMaxLength = 100;

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = null!;

    private StageName() { } // EF Core

    public StageName(Guid userId, string name)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("The stage name must belong to a user.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("The stage name is required.", nameof(name));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        Name = name.Trim();
    }
}
