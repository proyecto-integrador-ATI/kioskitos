using Kioskitos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kioskitos.Infrastructure.Persistence.Configurations;

public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        // Many clients per user. The FK also creates an index on UserId (used to filter by owner).
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(c => c.Name).HasMaxLength(Client.NameMaxLength).IsRequired();
        builder.Property(c => c.Industry).HasMaxLength(Client.IndustryMaxLength);
        builder.Property(c => c.Notes).HasMaxLength(Client.NotesMaxLength);

        builder.HasMany(c => c.Contacts)
            .WithOne()
            .HasForeignKey(cc => cc.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        // EF writes to the private _contacts list instead of the read-only property.
        builder.Navigation(c => c.Contacts).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}