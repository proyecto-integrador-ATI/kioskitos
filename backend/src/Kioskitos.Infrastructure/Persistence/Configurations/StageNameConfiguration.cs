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

        builder.Property(sn => sn.Name).HasMaxLength(StageName.NameMaxLength).IsRequired();

        builder.HasIndex(sn => new { sn.UserId, sn.Name }).IsUnique();
    }
}
