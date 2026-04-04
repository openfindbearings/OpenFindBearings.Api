using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenFindBearings.Domain.Entities;
using OpenFindBearings.Domain.Enums;

namespace OpenFindBearings.Infrastructure.Persistence.Configurations
{
    public class LicenseVerificationConfiguration : IEntityTypeConfiguration<LicenseVerification>
    {
        public void Configure(EntityTypeBuilder<LicenseVerification> builder)
        {
            builder.ToTable("LicenseVerifications");

            builder.HasKey(lv => lv.Id);

            //// 商家关系（只配置一次）
            //builder.HasOne(lv => lv.Merchant)
            //    .WithMany()
            //    .HasForeignKey(lv => lv.MerchantId)
            //    .OnDelete(DeleteBehavior.Restrict);

            // 提交人关系
            builder.HasOne(lv => lv.Submitter)
                .WithMany()
                .HasForeignKey(lv => lv.SubmittedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // 审核人关系
            builder.HasOne(lv => lv.Reviewer)
                .WithMany()
                .HasForeignKey(lv => lv.ReviewedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Status 枚举配置
            builder.Property(lv => lv.Status)
                .HasConversion<int>()
                .HasDefaultValue(LicenseVerificationStatus.Pending)
                .IsRequired();

            // 索引
            builder.HasIndex(lv => lv.MerchantId);
            builder.HasIndex(lv => lv.Status);
            builder.HasIndex(lv => lv.SubmittedAt);
        }
    }
}
