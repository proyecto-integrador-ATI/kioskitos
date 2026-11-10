namespace Kioskitos.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; }
    public string GoogleSubject { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Industry { get; private set; }
    public decimal? HourlyRate { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User() { } // EF Core

    public User(string googleSubject, string email, string name)
    {
        Id = Guid.NewGuid();
        GoogleSubject = googleSubject;
        Email = email;
        Name = name;
        CreatedAt = DateTime.UtcNow;
    }
}