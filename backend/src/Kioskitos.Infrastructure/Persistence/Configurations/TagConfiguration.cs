using Kioskitos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kioskitos.Infrastructure.Persistence.Configurations;

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("tags");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // citext = case-insensitive text: "Web" and "web" are the same tag,
        // both for the unique index below and for lookups by name.
        builder.Property(t => t.Name)
            .HasColumnType("citext")
            .HasMaxLength(Tag.NameMaxLength)
            .IsRequired();

        // A user cannot have the same tag twice (ignoring case). Also serves the lookup by user.
        builder.HasIndex(t => new { t.UserId, t.Name }).IsUnique();
    }
}
