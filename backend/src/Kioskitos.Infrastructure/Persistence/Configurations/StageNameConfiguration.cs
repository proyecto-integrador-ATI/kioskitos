using Kioskitos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kioskitos.Infrastructure.Persistence.Configurations;

public sealed class StageNameConfiguration : IEntityTypeConfiguration<StageName>
{
    public void Configure(EntityTypeBuilder<StageName> builder)
    {
        builder.ToTable("stage_names");

        builder.HasKey(sn => sn.Id);
        builder.Property(sn => sn.Id).ValueGeneratedNever();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(sn => sn.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // citext = case-insensitive text: "Design" and "design" are the same stage name,
        // both for the unique index below and for lookups by name.
        builder.Property(sn => sn.Name)
            .HasColumnType("citext")
            .HasMaxLength(StageName.NameMaxLength)
            .IsRequired();

        // A user cannot have the same stage name twice (ignoring case).
        builder.HasIndex(sn => new { sn.UserId, sn.Name }).IsUnique();
    }
}
