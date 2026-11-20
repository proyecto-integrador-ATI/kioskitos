using Kioskitos.Domain.Entities;

namespace Kioskitos.Application.Users;

public sealed record UserProfileDto(
    Guid Id, string Email, string Name, string? Industry, decimal? HourlyRate, DateTime CreatedAt)
{
    public static UserProfileDto From(User user) =>
        new(user.Id, user.Email, user.Name, user.Industry, user.HourlyRate, user.CreatedAt);
}

public sealed record UpdateUserProfileCommand(string Name, string? Industry, decimal? HourlyRate);

public sealed class UserProfileService(IUserRepository users)
{
    public async Task<UserProfileDto?> GetAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(userId, ct);
        return user is null ? null : UserProfileDto.From(user);
    }

    /// <summary>Updates the profile of the given user. Returns null if the user does not exist.</summary>
    public async Task<UserProfileDto?> UpdateAsync(
        Guid userId, UpdateUserProfileCommand command, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null)
        {
            return null;
        }

        user.UpdateProfile(command.Name, command.Industry, command.HourlyRate);
        await users.SaveChangesAsync(ct);

        return UserProfileDto.From(user);
    }
}
