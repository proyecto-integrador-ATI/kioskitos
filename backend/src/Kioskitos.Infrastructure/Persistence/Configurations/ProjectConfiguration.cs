using Kioskitos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kioskitos.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    // The longest ProjectStatus name is "InProgress" (10 chars); 20 leaves room for new values.
    internal const int StatusMaxLength = 20;

    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many projects per client. The FK also creates the index used by the list by client.
        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(p => p.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.Name).HasMaxLength(Project.NameMaxLength).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(Project.TextMaxLength);
        builder.Property(p => p.Objectives).HasMaxLength(Project.TextMaxLength);
        builder.Property(p => p.Scope).HasMaxLength(Project.TextMaxLength);

        // Stored as text ("InProgress") so the column stays readable and reordering the enum is safe.
        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(StatusMaxLength)
            .IsRequired();

        builder.HasMany(p => p.Stages)
            .WithOne()
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Tags)
            .WithOne()
            .HasForeignKey(pt => pt.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // EF writes to the private _stages and _tags lists instead of the read-only properties.
        builder.Navigation(p => p.Stages).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Tags).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
