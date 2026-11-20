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

    /// <summary>
    /// Updates the editable profile fields. Identity and audit data
    /// (Id, GoogleSubject, Email, CreatedAt) are never modified here.
    /// </summary>
    public void UpdateProfile(string name, string? industry, decimal? hourlyRate)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (hourlyRate < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hourlyRate), hourlyRate, "Hourly rate must be greater than or equal to zero.");
        }

        Name = name.Trim();
        Industry = string.IsNullOrWhiteSpace(industry) ? null : industry.Trim();
        HourlyRate = hourlyRate;
    }
}