using Kioskitos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kioskitos.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.GoogleSubject).HasMaxLength(255).IsRequired();
        builder.HasIndex(u => u.GoogleSubject).IsUnique();

        builder.Property(u => u.Email).HasMaxLength(320).IsRequired();
        builder.Property(u => u.Name).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Industry).HasMaxLength(100);
        builder.Property(u => u.HourlyRate).HasPrecision(12, 2);
    }
}