using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Aggregates;
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
                .HasConversion<int>()
                .HasDefaultValue(MerchantGrade.Standard)
                .HasSentinel(MerchantGrade.Unknown)  // 当值为 Unknown 时，使用数据库默认值
                .HasColumnName("Grade");

            // ============ 值对象 - DataSource ============
            builder.OwnsOne(m => m.DataSource, ds =>
            {
                ds.Property(d => d.SourceType)
                    .HasColumnName("DataSourceType")
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired(false);

                ds.Property(d => d.CrawlerSite)
                    .HasColumnName("CrawlerSite")
                    .HasConversion<int?>();

                ds.Property(d => d.SourceUrl)
                    .HasColumnName("SourceUrl")
                    .HasMaxLength(1000);

                ds.Property(d => d.SourceDetail)
                    .HasColumnName("SourceDetail")
                    .HasMaxLength(500);

                ds.Property(d => d.SourceId)
                    .HasColumnName("SourceId")
                    .HasMaxLength(200);

                ds.Property(d => d.ImportedBy)
                    .HasColumnName("ImportedBy")
                    .HasMaxLength(100);

                ds.Property(d => d.ImportedAt)
                    .HasColumnName("ImportedAt");

                ds.Property(d => d.ReliabilityScore)
                    .HasColumnName("ReliabilityScore");
            });

            // ============ 数据追溯字段 ============
            builder.Property(m => m.LastVerifiedAt)
                .HasColumnName("LastVerifiedAt");

            builder.Property(m => m.VerifiedBy)
                .HasMaxLength(100)
                .HasColumnName("VerifiedBy");

            builder.Property(m => m.IsDataVerified)
                .HasDefaultValue(false)
                .HasColumnName("IsDataVerified");

            builder.Property(m => m.DataRemark)
                .HasMaxLength(2000)
                .HasColumnName("DataRemark");

            // ============ 统计字段 ============
            builder.Property(m => m.ProductCount)
                .HasDefaultValue(0)
                .HasColumnName("ProductCount");

            builder.Property(m => m.FollowerCount)
                .HasDefaultValue(0)
                .HasColumnName("FollowerCount");

            builder.Property(m => m.ViewCount)
                .HasDefaultValue(0)
                .HasColumnName("ViewCount");

            // ============ 状态字段 ============
            builder.Property(m => m.Status)
                .HasConversion<int>()
                .HasDefaultValue(MerchantStatus.Pending)
                .HasSentinel(MerchantStatus.Pending)
                .HasColumnName("Status");

            builder.Property(m => m.SuspensionReason)
                .HasMaxLength(500)
                .HasColumnName("SuspensionReason");

            // ============ Logo和官网 ============
            builder.Property(m => m.LogoUrl)
                .HasMaxLength(500)
                .HasColumnName("LogoUrl");

            builder.Property(m => m.Website)
                .HasMaxLength(200)
                .HasColumnName("Website");

            // ============ 统一社会信用代码 ============
            builder.Property(m => m.UnifiedSocialCreditCode)
                .HasMaxLength(18)
                .HasColumnName("UnifiedSocialCreditCode");

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

            builder.HasIndex(m => m.Status);
            builder.HasIndex(m => m.UnifiedSocialCreditCode);

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
