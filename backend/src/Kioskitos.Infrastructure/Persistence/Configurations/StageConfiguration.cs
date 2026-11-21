using Kioskitos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kioskitos.Infrastructure.Persistence.Configurations;

public sealed class StageConfiguration : IEntityTypeConfiguration<Stage>
{
    public void Configure(EntityTypeBuilder<Stage> builder)
    {
        builder.ToTable("stages");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        // Optional: the generic stage has no name. A name in use cannot be deleted.
        builder.HasOne<StageName>()
            .WithMany()
            .HasForeignKey(s => s.StageNameId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.Description).HasMaxLength(Stage.DescriptionMaxLength);
        builder.Property(s => s.Deliverable).HasMaxLength(Stage.DeliverableMaxLength);
        builder.Property(s => s.EstimatedHours).HasPrecision(8, 2);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(ProjectConfiguration.StatusMaxLength)
            .IsRequired();
    }
}
