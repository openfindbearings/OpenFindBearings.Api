using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;

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

            builder.Property(m => m.Type)
                .IsRequired();

            builder.Property(m => m.Description)
                .HasMaxLength(1000);

            builder.Property(m => m.BusinessScope)
                .HasMaxLength(500);

            builder.Property(m => m.IsVerified)
                .HasDefaultValue(false);

            builder.Property(m => m.Grade)
                .HasDefaultValue(MerchantGrade.Regular);

            // 值对象配置 - ContactInfo
            builder.OwnsOne(m => m.Contact, contact =>
            {
                contact.Property(c => c.ContactPerson).HasColumnName("ContactPerson").HasMaxLength(100);
                contact.Property(c => c.Phone).HasColumnName("Phone").HasMaxLength(20);
                contact.Property(c => c.Mobile).HasColumnName("Mobile").HasMaxLength(20);
                contact.Property(c => c.Email).HasColumnName("Email").HasMaxLength(100);
                contact.Property(c => c.Address).HasColumnName("Address").HasMaxLength(500);
            });

            // 索引
            builder.HasIndex(m => m.Name);
            builder.HasIndex(m => m.Type);
            builder.HasIndex(m => m.IsVerified);
            builder.HasIndex(m => m.Grade);

            // 导航属性 - 员工（一对多）
            builder.HasMany(m => m.Staff)
                .WithOne(u => u.Merchant)
                .HasForeignKey(u => u.MerchantId)
                .OnDelete(DeleteBehavior.Restrict);

            // 导航属性 - 产品关联（一对多）
            builder.HasMany(m => m.MerchantBearings)
                .WithOne(mb => mb.Merchant)
                .HasForeignKey(mb => mb.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);

            // 导航属性 - 粉丝（一对多）
            builder.HasMany(m => m.FollowedByUsers)
                .WithOne(uf => uf.Merchant)
                .HasForeignKey(uf => uf.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
