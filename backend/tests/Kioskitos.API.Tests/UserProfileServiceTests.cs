using Kioskitos.Application.Users;
using Kioskitos.Domain.Entities;

namespace Kioskitos.API.Tests;

public sealed class UserProfileServiceTests
{
    [Fact]
    public async Task Get_ExistingUser_ReturnsProfile()
    {
        var user = new User("google-sub", "user@example.com", "Test User");
        var service = new UserProfileService(new InMemoryUserRepository(user));

        var profile = await service.GetAsync(user.Id, CancellationToken.None);

        Assert.NotNull(profile);
        Assert.Equal(user.Id, profile!.Id);
        Assert.Equal("user@example.com", profile.Email);
        Assert.Equal("Test User", profile.Name);
    }

    [Fact]
    public async Task Get_UnknownUser_ReturnsNull()
    {
        var service = new UserProfileService(new InMemoryUserRepository());

        Assert.Null(await service.GetAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Update_ExistingUser_UpdatesAndSaves()
    {
        var user = new User("google-sub", "user@example.com", "Test User");
        var users = new InMemoryUserRepository(user);
        var service = new UserProfileService(users);

        var profile = await service.UpdateAsync(
            user.Id, new UpdateUserProfileCommand("New Name", "Software", 40m), CancellationToken.None);

        Assert.NotNull(profile);
        Assert.Equal("New Name", profile!.Name);
        Assert.Equal("Software", profile.Industry);
        Assert.Equal(40m, profile.HourlyRate);
        Assert.Equal("user@example.com", profile.Email);
        Assert.Equal(1, users.SaveChangesCalls);
    }

    [Fact]
    public async Task Update_OnlyModifiesTheGivenUser()
    {
        var user = new User("sub-1", "one@example.com", "User One");
        var other = new User("sub-2", "two@example.com", "User Two");
        var service = new UserProfileService(new InMemoryUserRepository(user, other));

        await service.UpdateAsync(
            user.Id, new UpdateUserProfileCommand("Changed", null, null), CancellationToken.None);

        Assert.Equal("Changed", user.Name);
        Assert.Equal("User Two", other.Name);
    }

    [Fact]
    public async Task Update_UnknownUser_ReturnsNullAndDoesNotSave()
    {
        var users = new InMemoryUserRepository();
        var service = new UserProfileService(users);

        var profile = await service.UpdateAsync(
            Guid.NewGuid(), new UpdateUserProfileCommand("Name", null, null), CancellationToken.None);

        Assert.Null(profile);
        Assert.Equal(0, users.SaveChangesCalls);
    }
}

internal sealed class InMemoryUserRepository(params User[] users) : IUserRepository
{
    public List<User> Users { get; } = [.. users];
    public int SaveChangesCalls { get; private set; }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetByGoogleSubjectAsync(string googleSubject, CancellationToken ct) =>
        Task.FromResult(Users.FirstOrDefault(u => u.GoogleSubject == googleSubject));

    public Task AddAsync(User user, CancellationToken ct)
    {
        Users.Add(user);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveChangesCalls++;
        return Task.CompletedTask;
    }
}
