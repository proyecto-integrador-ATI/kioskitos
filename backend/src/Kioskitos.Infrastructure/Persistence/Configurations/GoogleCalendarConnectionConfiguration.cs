using Kioskitos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kioskitos.Infrastructure.Persistence.Configurations;

public sealed class GoogleCalendarConnectionConfiguration
    : IEntityTypeConfiguration<GoogleCalendarConnection>
{
    public void Configure(EntityTypeBuilder<GoogleCalendarConnection> builder)
    {
        builder.ToTable("google_calendar_connections");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        // One-to-one with User: creates a unique index on UserId and deletes the connection when the user is deleted.
        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<GoogleCalendarConnection>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(c => c.EncryptedRefreshToken).IsRequired();
        builder.Property(c => c.CalendarId).HasMaxLength(255).IsRequired();
        builder.Property(c => c.GrantedScopes).HasMaxLength(1000).IsRequired();
    }
}