using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    public class MerchantConfiguration : IEntityTypeConfiguration<Merchant>
    {
        public void Configure(EntityTypeBuilder<Merchant> builder)
        {
            builder.ToTable("Merchants");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(m => m.CompanyName)
                .HasMaxLength(200);

            builder.Property(m => m.Description)
                .HasMaxLength(1000);

            builder.Property(m => m.BusinessScope)
                .HasMaxLength(500);

            // 值对象配置 - ContactInfo作为复杂类型
            builder.OwnsOne(m => m.Contact, contact =>
            {
                contact.Property(c => c.ContactPerson).HasMaxLength(100);
                contact.Property(c => c.Phone).HasMaxLength(20);
                contact.Property(c => c.Mobile).HasMaxLength(20);
                contact.Property(c => c.Email).HasMaxLength(100);
                contact.Property(c => c.Address).HasMaxLength(500);
            });

            builder.HasIndex(m => m.Name);
        }
    }
}
