using Kioskitos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kioskitos.Infrastructure.Persistence.Configurations;

public sealed class ClientContactConfiguration : IEntityTypeConfiguration<ClientContact>
{
    public void Configure(EntityTypeBuilder<ClientContact> builder)
    {
        builder.ToTable("client_contacts");

        builder.HasKey(cc => cc.Id);
        builder.Property(cc => cc.Id).ValueGeneratedNever();

        builder.Property(cc => cc.Name).HasMaxLength(ClientContact.NameMaxLength).IsRequired();
        builder.Property(cc => cc.Email).HasMaxLength(ClientContact.EmailMaxLength).IsRequired();
        builder.Property(cc => cc.Phone).HasMaxLength(ClientContact.PhoneMaxLength).IsRequired();
    }
}