using Kioskitos.Domain.Entities;

namespace Kioskitos.API.Tests;

public sealed class UserTests
{
    private static User CreateUser() => new("google-sub", "user@example.com", "Original Name");

    [Fact]
    public void UpdateProfile_WithValidData_UpdatesEditableFields()
    {
        var user = CreateUser();

        user.UpdateProfile("New Name", "Software", 50.5m);

        Assert.Equal("New Name", user.Name);
        Assert.Equal("Software", user.Industry);
        Assert.Equal(50.5m, user.HourlyRate);
    }

    [Fact]
    public void UpdateProfile_DoesNotModifyIdentityOrAuditFields()
    {
        var user = CreateUser();
        var id = user.Id;
        var createdAt = user.CreatedAt;

        user.UpdateProfile("New Name", "Software", 50m);

        Assert.Equal(id, user.Id);
        Assert.Equal("google-sub", user.GoogleSubject);
        Assert.Equal("user@example.com", user.Email);
        Assert.Equal(createdAt, user.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdateProfile_WithoutName_ThrowsAndKeepsPreviousValues(string? name)
    {
        var user = CreateUser();

        Assert.Throws<ArgumentException>(() => user.UpdateProfile(name!, "Software", 10m));

        Assert.Equal("Original Name", user.Name);
        Assert.Null(user.Industry);
        Assert.Null(user.HourlyRate);
    }

    [Fact]
    public void UpdateProfile_WithNegativeHourlyRate_ThrowsAndKeepsPreviousValues()
    {
        var user = CreateUser();

        Assert.Throws<ArgumentOutOfRangeException>(() => user.UpdateProfile("New Name", "Software", -1m));

        Assert.Equal("Original Name", user.Name);
        Assert.Null(user.HourlyRate);
    }

    [Fact]
    public void UpdateProfile_WithZeroHourlyRate_IsAllowed()
    {
        var user = CreateUser();

        user.UpdateProfile("New Name", null, 0m);

        Assert.Equal(0m, user.HourlyRate);
    }

    [Fact]
    public void UpdateProfile_WithNullOptionalFields_ClearsThem()
    {
        var user = CreateUser();
        user.UpdateProfile("Name", "Software", 50m);

        user.UpdateProfile("Name", null, null);

        Assert.Null(user.Industry);
        Assert.Null(user.HourlyRate);
    }

    [Fact]
    public void UpdateProfile_TrimsValuesAndNormalizesBlankIndustryToNull()
    {
        var user = CreateUser();

        user.UpdateProfile("  New Name  ", "   ", null);

        Assert.Equal("New Name", user.Name);
        Assert.Null(user.Industry);
    }

    [Fact]
    public void UpdateProfile_TrimsIndustry()
    {
        var user = CreateUser();

        user.UpdateProfile("Name", "  Software  ", null);

        Assert.Equal("Software", user.Industry);
    }
}
